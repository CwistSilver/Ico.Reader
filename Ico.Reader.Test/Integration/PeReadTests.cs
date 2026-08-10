namespace Ico.Reader.Test.Integration;

/// <summary>
/// Reads icons and cursors back out of a real PE file. The fixture assembly embeds four
/// RT_GROUP_ICON groups (ids 1..4) and three RT_GROUP_CURSOR groups (ids 1..3), which is the only
/// way to reach the multi-group code path that standalone ICO and CUR files cannot exercise.
/// </summary>
public sealed class PeReadTests
{
    private readonly IcoReader _reader = new();

    private IcoData Read()
    {
        var ico = _reader.Read(TestFiles.PeFixture);
        Assert.NotNull(ico);
        return ico;
    }

    [Fact]
    public void Read_RecognisesTheFileAsADll()
    {
        var ico = Read();

        Assert.Equal(IcoOriginFileType.Dll, ico.OriginFileType);
    }

    [Fact]
    public void Read_FindsEveryIconGroup()
    {
        var ico = Read();

        Assert.Equal(["1", "2", "3", "4"], ico.IconGroups.Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal));
        Assert.All(ico.IconGroups, group => Assert.Equal(IcoType.Icon, group.IcoType));
    }

    [Fact]
    public void Read_FindsEveryCursorGroup()
    {
        var ico = Read();

        Assert.Equal(["1", "2", "3"], ico.CursorGroups.Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal));
        Assert.All(ico.CursorGroups, group => Assert.Equal(IcoType.Cursor, group.IcoType));
    }

    [Fact]
    public void Read_KeepsIconAndCursorGroupsApartDespiteSharedNames()
    {
        // Icon group "1" and cursor group "1" coexist; only the type tells them apart.
        var ico = Read();

        Assert.Equal(ico.IconGroups.Count + ico.CursorGroups.Count, ico.Groups.Count);
        Assert.Equal(2, ico.GetGroups("1").Count());
        Assert.Equal(IcoType.Icon, ico.GetGroup("1", IcoType.Icon).IcoType);
        Assert.Equal(IcoType.Cursor, ico.GetGroup("1", IcoType.Cursor).IcoType);
    }

    [Fact]
    public void Read_KeepsGroupSizesSeparate()
    {
        var ico = Read();

        Assert.Equal(1, ico.GetIconGroup("1").Size);
        Assert.Equal(3, ico.GetIconGroup("2").Size);
        Assert.Equal(1, ico.GetIconGroup("3").Size);
        Assert.Equal(3, ico.GetIconGroup("4").Size);
    }

    [Fact]
    public void Read_ExposesEveryIconAndCursorAsAnImageReference()
    {
        var ico = Read();

        Assert.Equal(8, ico.ImageReferences.Count(x => x.IcoType == IcoType.Icon));
        Assert.Equal(5, ico.ImageReferences.Count(x => x.IcoType == IcoType.Cursor));
        Assert.Equal(13, ico.ImageReferences.Count);
    }

    [Fact]
    public void Read_KeepsCursorGroupSizesSeparate()
    {
        var ico = Read();

        Assert.Equal(1, ico.GetCursorGroup("1").Size);
        Assert.Equal(3, ico.GetCursorGroup("2").Size);
        Assert.Equal(1, ico.GetCursorGroup("3").Size);
    }

    [Fact]
    public void Read_ReadsCursorHotspotsFromTheResourcePrefix()
    {
        // Inside a PE the hotspot is a four byte prefix on the RT_CURSOR data rather than a field
        // in the group directory.
        var ico = Read();

        var single = Assert.Single(ico.GetImageReferences("1", IcoType.Cursor));
        Assert.Equal(10, single.HotspotX);
        Assert.Equal(20, single.HotspotY);

        var multi = ico.GetImageReferences("2", IcoType.Cursor).OrderBy(x => x.Width).ToArray();
        Assert.Equal([(4, 6), (8, 12), (12, 18)], multi.Select(x => ((int)x.HotspotX, (int)x.HotspotY)));
    }

    [Fact]
    public void Read_ResolvesCursorImageSizes()
    {
        var ico = Read();

        var multi = ico.GetImageReferences("2", IcoType.Cursor).OrderBy(x => x.Width).ToArray();

        Assert.Equal([16, 32, 48], multi.Select(x => x.Width));
        Assert.Equal([16, 32, 48], multi.Select(x => x.Height));
    }

    [Fact]
    public void Read_ReadsCursorGroupEntryDimensions()
    {
        // A cursor group stores these as words with a doubled height, where an icon group uses two
        // bytes; reading the icon layout here would report a height of 0.
        var ico = Read();

        var entries = ico.GetCursorGroup("2").DirectoryEntries.OrderBy(x => x.Width).ToArray();

        Assert.Equal([16, 32, 48], entries.Select(x => (int)x.Width));
        Assert.Equal([16, 32, 48], entries.Select(x => (int)x.Height));
        Assert.All(entries, entry => Assert.Equal(32, entry.ColorDepth));
    }

    [Fact]
    public void Read_MatchesTheStandaloneCursorGroupEntry()
    {
        var ico = Read();
        var fromPe = Assert.Single(ico.GetCursorGroup("3").DirectoryEntries);

        var standalone = new IcoReader().Read(TestFiles.Cur("cursor_16_8bpp.cur"));
        Assert.NotNull(standalone);
        var fromFile = Assert.Single(((CursorGroup)standalone.Groups[0]).DirectoryEntries);

        Assert.Equal(fromFile.Width, fromPe.Width);
        Assert.Equal(fromFile.Height, fromPe.Height);
        Assert.Equal(fromFile.HotspotX, fromPe.HotspotX);
        Assert.Equal(fromFile.HotspotY, fromPe.HotspotY);
    }

    [Fact]
    public void GetImage_DecodesEveryCursorGroupEntry()
    {
        var ico = Read();

        foreach (var group in ico.CursorGroups)
        {
            for (var i = 0; i < group.Size; i++)
            {
                var reference = ico.GetImageReference(group, i);
                var png = PngImage.Parse(ico.GetImage(group, i));

                Assert.Equal(reference.Width, png.Width);
                Assert.Equal(reference.Height, png.Height);
            }
        }
    }

    [Fact]
    public void GetImage_MatchesTheStandaloneCursorFile()
    {
        // Cursor group 3 is cursor_16_8bpp.cur; the PE and file paths must decode it identically.
        var ico = Read();
        var fromPe = ico.GetImage(ico.GetCursorGroup("3"), 0);

        var standalone = new IcoReader().Read(TestFiles.Cur("cursor_16_8bpp.cur"));

        Assert.NotNull(standalone);
        Assert.Equal(standalone.GetImage(0), fromPe);
    }

    [Fact]
    public void GetImageReferences_ResolvesGroupEntriesToTheirImages()
    {
        var ico = Read();

        var references = ico.GetImageReferences("2", IcoType.Icon);

        Assert.Equal([16, 32, 48], references.Select(x => x.Width).OrderBy(x => x));
    }

    [Fact]
    public void GetImage_DecodesEveryGroupEntry()
    {
        var ico = Read();

        foreach (var group in ico.Groups)
        {
            for (var i = 0; i < group.Size; i++)
            {
                var reference = ico.GetImageReference(group, i);
                var png = PngImage.Parse(ico.GetImage(group, i));

                Assert.Equal(reference.Width, png.Width);
                Assert.Equal(reference.Height, png.Height);
            }
        }
    }

    [Fact]
    public void GetImage_DecodesThePngCompressedEntryInAGroup()
    {
        var ico = Read();

        var reference = ico.GetImageReferences("4", IcoType.Icon).Single(x => x.Format == IcoImageFormat.PNG);

        Assert.Equal(256, reference.Width);
        Assert.Equal(File.ReadAllBytes(TestFiles.SamplePng), ico.GetImage(reference));
    }

    [Fact]
    public void GetImageReference_RejectsAnIndexOutsideTheGroup()
    {
        var ico = Read();
        var singleEntryGroup = ico.GetIconGroup("1");

        // The global reference list has eight entries; the guard has to use the group's own size.
        Assert.Throws<ArgumentOutOfRangeException>(() => ico.GetImageReference(singleEntryGroup, 1));
    }

    [Fact]
    public void PreferredImageIndex_ScopesToTheRequestedGroup()
    {
        var ico = Read();

        var index = ico.PreferredImageIndex(ico.GetIconGroup("2"));

        Assert.Equal(48, ico.ImageReferences[index].Width);
    }

    [Fact]
    public async Task GetImageAsync_MatchesGetImageForPeEntries()
    {
        var ico = Read();

        foreach (var reference in ico.ImageReferences)
            Assert.Equal(ico.GetImage(reference), await ico.GetImageAsync(reference));
    }
}
