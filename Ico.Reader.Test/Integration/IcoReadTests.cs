namespace Ico.Reader.Test.Integration;

public sealed class IcoReadTests
{
    private readonly IcoReader _reader = new();

    [Theory]
    [MemberData(nameof(IcoFixtures.SingleImage), MemberType = typeof(IcoFixtures))]
    public void Read_ExposesTheDeclaredMetadata(string file, int width, int height, int bitCount)
    {
        var ico = _reader.Read(TestFiles.Ico(file));

        Assert.NotNull(ico);
        var reference = Assert.Single(ico.ImageReferences);
        Assert.Equal(width, reference.Width);
        Assert.Equal(height, reference.Height);
        Assert.Equal(bitCount, reference.BitCount);
        Assert.Equal(IcoImageFormat.Bmp, reference.Format);
        Assert.Equal(IcoType.Icon, reference.IcoType);
    }

    [Theory]
    [MemberData(nameof(IcoFixtures.All), MemberType = typeof(IcoFixtures))]
    public void Read_PutsEveryImageInASingleGroup(string file, int entries)
    {
        var ico = _reader.Read(TestFiles.Ico(file));

        Assert.NotNull(ico);
        Assert.Equal(IcoOriginFileType.Ico, ico.OriginFileType);
        Assert.Equal(entries, ico.ImageReferences.Count);

        var group = Assert.Single(ico.Groups);
        Assert.Equal("1", group.Name);
        Assert.Equal(IcoType.Icon, group.IcoType);
        Assert.Equal(entries, group.Size);
        Assert.Single(ico.IconGroups);
        Assert.Empty(ico.CursorGroups);
    }

    [Theory]
    [MemberData(nameof(IcoFixtures.All), MemberType = typeof(IcoFixtures))]
    public void Read_NumbersImageReferencesInOrder(string file, int entries)
    {
        var ico = _reader.Read(TestFiles.Ico(file));

        Assert.NotNull(ico);
        Assert.Equal(Enumerable.Range(0, entries), ico.ImageReferences.Select(x => x.Id));
    }

    [Theory]
    [MemberData(nameof(IcoFixtures.All), MemberType = typeof(IcoFixtures))]
    public void GetImage_DecodesEveryEntryToAValidPng(string file, int entries)
    {
        var ico = _reader.Read(TestFiles.Ico(file));

        Assert.NotNull(ico);
        for (var i = 0; i < entries; i++)
        {
            var reference = ico.ImageReferences[i];
            var png = PngImage.Parse(ico.GetImage(i));

            Assert.Equal(reference.Width, png.Width);
            Assert.Equal(reference.Height, png.Height);
        }
    }

    [Fact]
    public void Read_ResolvesA256PixelIconFromTheImageHeader()
    {
        // The directory entry stores 0 for 256, so the real size has to come from the image itself.
        var ico = _reader.Read(TestFiles.Ico("icon_256_32bpp.ico"));

        Assert.NotNull(ico);
        var reference = Assert.Single(ico.ImageReferences);
        Assert.Equal(256, reference.Width);
        Assert.Equal(256, reference.Height);
    }

    [Fact]
    public void Read_DetectsPngCompressedEntries()
    {
        var ico = _reader.Read(TestFiles.Ico(IcoFixtures.PngEmbedded));

        Assert.NotNull(ico);
        var reference = Assert.Single(ico.ImageReferences);
        Assert.Equal(IcoImageFormat.Png, reference.Format);
        Assert.Equal(256, reference.Width);
        Assert.Equal(256, reference.Height);
    }

    [Fact]
    public void GetImage_PassesPngEntriesThroughUnchanged()
    {
        var ico = _reader.Read(TestFiles.Ico(IcoFixtures.PngEmbedded));
        Assert.NotNull(ico);

        var decoded = ico.GetImage(0);

        Assert.Equal(File.ReadAllBytes(TestFiles.SamplePng), decoded);
    }

    [Fact]
    public void Read_SetsTheNameFromTheFileName()
    {
        var ico = _reader.Read(TestFiles.Ico("icon_32_8bpp.ico"));

        Assert.NotNull(ico);
        Assert.Equal("icon_32_8bpp", ico.Name);
    }

    [Fact]
    public void ToString_SummarisesTheContent()
    {
        var ico = _reader.Read(TestFiles.Ico("icon_multi.ico"));

        Assert.NotNull(ico);
        Assert.Equal("icon_multi Groups[1] Images[3] (Ico)", ico.ToString());
    }
}
