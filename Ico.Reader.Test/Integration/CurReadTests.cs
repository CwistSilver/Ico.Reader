namespace Ico.Reader.Test.Integration;

public sealed class CurReadTests
{
    private readonly IcoReader _reader = new();

    private IcoData Read(string file)
    {
        var ico = _reader.Read(TestFiles.Cur(file));
        Assert.NotNull(ico);
        return ico;
    }

    [Theory]
    [MemberData(nameof(CurFixtures.All), MemberType = typeof(CurFixtures))]
    public void Read_RecognisesTheFileAsACursor(string file, CursorImage[] images)
    {
        var cur = Read(file);

        Assert.Equal(IcoOriginFileType.Cur, cur.OriginFileType);
        Assert.Equal(images.Length, cur.ImageReferences.Count);

        var group = Assert.Single(cur.Groups);
        Assert.Equal(IcoType.Cursor, group.IcoType);
        Assert.Equal("1", group.Name);
        Assert.Single(cur.CursorGroups);
        Assert.Empty(cur.IconGroups);
    }

    [Theory]
    [MemberData(nameof(CurFixtures.AllFiles), MemberType = typeof(CurFixtures))]
    public void Read_MarksEveryImageAsACursor(string file)
    {
        var cur = Read(file);

        Assert.NotEmpty(cur.ImageReferences);
        Assert.All(cur.ImageReferences, reference => Assert.Equal(IcoType.Cursor, reference.IcoType));
    }

    [Theory]
    [MemberData(nameof(CurFixtures.SingleImage), MemberType = typeof(CurFixtures))]
    public void Read_ExposesTheDeclaredMetadata(string file, CursorImage image)
    {
        var cur = Read(file);

        var reference = Assert.Single(cur.ImageReferences);
        Assert.Equal(image.Width, reference.Width);
        Assert.Equal(image.Height, reference.Height);
        Assert.Equal(image.BitCount, reference.BitCount);
    }

    [Theory]
    [MemberData(nameof(CurFixtures.SingleImage), MemberType = typeof(CurFixtures))]
    public void Read_ReadsTheHotspot(string file, CursorImage image)
    {
        var cur = Read(file);

        var reference = Assert.Single(cur.ImageReferences);
        Assert.Equal(image.HotspotX, reference.HotspotX);
        Assert.Equal(image.HotspotY, reference.HotspotY);
    }

    [Theory]
    [MemberData(nameof(CurFixtures.MultiImage), MemberType = typeof(CurFixtures))]
    public void Read_KeepsAPerImageHotspot(string file, CursorImage[] images)
    {
        // A cursor stores one hotspot per image; scaled variants do not share it.
        var cur = Read(file);

        Assert.Equal(images.Select(x => (x.HotspotX, x.HotspotY)), cur.ImageReferences.Select(x => ((int)x.HotspotX, (int)x.HotspotY)));
        Assert.Equal(images.Select(x => x.Width), cur.ImageReferences.Select(x => x.Width));
        Assert.Equal(images.Select(x => x.BitCount), cur.ImageReferences.Select(x => x.BitCount));
    }

    [Theory]
    [MemberData(nameof(CurFixtures.All), MemberType = typeof(CurFixtures))]
    public void Read_CopiesTheHotspotOntoTheDirectoryEntries(string file, CursorImage[] images)
    {
        var cur = Read(file);
        var group = Assert.IsType<CursorGroup>(cur.Groups[0]);

        Assert.Equal(images.Select(x => (x.HotspotX, x.HotspotY)), group.DirectoryEntries.Select(x => ((int)x.HotspotX, (int)x.HotspotY)));
    }

    [Theory]
    [MemberData(nameof(CurFixtures.All), MemberType = typeof(CurFixtures))]
    public void GetImage_DecodesEveryEntryToAValidPng(string file, CursorImage[] images)
    {
        var cur = Read(file);

        for (var i = 0; i < images.Length; i++)
        {
            var png = PngImage.Parse(cur.GetImage(i));

            Assert.Equal(images[i].Width, png.Width);
            Assert.Equal(images[i].Height, png.Height);
        }
    }

    [Theory]
    [MemberData(nameof(CurFixtures.All), MemberType = typeof(CurFixtures))]
    public void GetImage_MatchesTheReferenceDecoder(string file, CursorImage[] images)
    {
        var cur = Read(file);

        for (var i = 0; i < images.Length; i++)
        {
            var decoded = PngImage.Parse(cur.GetImage(i));

            PixelAssert.Matches(TestFiles.ExpectedPixels(file, i), decoded.Rgba, images[i].Width, images[i].Height);
        }
    }

    [Fact]
    public void Read_DetectsAPngCompressedCursor()
    {
        var cur = Read(CurFixtures.PngEmbedded);

        var reference = Assert.Single(cur.ImageReferences);
        Assert.Equal(IcoImageFormat.Png, reference.Format);
        Assert.Equal(IcoType.Cursor, reference.IcoType);
        Assert.Equal(256, reference.Width);
        Assert.Equal(128, reference.HotspotX);
        Assert.Equal(200, reference.HotspotY);
    }

    [Fact]
    public void GetImage_AppliesTheAndMask()
    {
        var cur = Read(CurFixtures.Masked);

        var image = PngImage.Parse(cur.GetImage(0));

        Assert.Equal(0, AlphaAt(image, 0, 0));
        Assert.Equal(0, AlphaAt(image, image.Width - 1, image.Height - 1));
        Assert.Equal(255, AlphaAt(image, image.Width / 2, image.Height / 2));
    }

    [Fact]
    public void GetCursorGroup_ResolvesTheGroup()
    {
        var cur = Read("cursor_multi.cur");

        var group = cur.GetCursorGroup("1");

        Assert.Equal(3, group.Size);
        Assert.Same(group, cur.GetGroup("1", IcoType.Cursor));
        Assert.Throws<InvalidOperationException>(() => cur.GetIconGroup("1"));
        Assert.Throws<InvalidOperationException>(() => cur.GetGroup("1", IcoType.Icon));
    }

    [Fact]
    public void GetImage_ByGroupMatchesGetImageByIndex()
    {
        var cur = Read("cursor_multi_mixed.cur");
        var group = cur.Groups[0];

        for (var i = 0; i < group.Size; i++)
            Assert.Equal(cur.GetImage(i), cur.GetImage(group, i));
    }

    [Fact]
    public async Task GetImageAsync_MatchesGetImage()
    {
        var cur = Read("cursor_multi.cur");

        foreach (var reference in cur.ImageReferences)
            Assert.Equal(cur.GetImage(reference), await cur.GetImageAsync(reference, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Read_ProducesTheSameImagesFromEverySource()
    {
        var path = TestFiles.Cur("cursor_multi_mixed.cur");
        var fromPath = _reader.Read(path);
        var fromBytes = _reader.Read(File.ReadAllBytes(path));

        Assert.NotNull(fromPath);
        Assert.NotNull(fromBytes);
        Assert.Equal(fromPath.OriginFileType, fromBytes.OriginFileType);

        for (var i = 0; i < fromPath.ImageReferences.Count; i++)
        {
            Assert.Equal(fromPath.GetImage(i), fromBytes.GetImage(i));
            Assert.Equal(fromPath.ImageReferences[i].HotspotX, fromBytes.ImageReferences[i].HotspotX);
        }
    }

    [Fact]
    public void PreferredImageIndex_PicksTheLargestCursor()
    {
        var cur = Read("cursor_multi_mixed.cur");

        var index = cur.PreferredImageIndex();

        Assert.Equal(48, cur.ImageReferences[index].Width);
    }

    [Fact]
    public void ToString_ReportsTheCursorOrigin()
    {
        var cur = Read("cursor_multi.cur");

        Assert.Equal("cursor_multi Groups[1] Images[3] (Cur)", cur.ToString());
    }

    private static byte AlphaAt(PngImage image, int x, int y) => image.Rgba[(((y * image.Width) + x) * 4) + 3];
}
