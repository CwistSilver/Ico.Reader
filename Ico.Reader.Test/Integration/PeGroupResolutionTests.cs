using Ico.Reader.PeDecoder.Models;

namespace Ico.Reader.Test.Integration;

/// <summary>
/// A group directory entry names a resource id rather than a file offset, and a file can name an id
/// it does not actually carry. Those entries have to be dropped without disturbing the ones around
/// them.
/// </summary>
public sealed class PeGroupResolutionTests
{
    private const int GroupHeaderSize = 6;
    private const int GroupEntrySize = 14;
    private const int ResourceIdField = 12;
    private const int BitmapInfoHeaderSize = 40;
    private const int CursorHotspotSize = 4;
    private const int LanguageCountField = 14;

    private readonly IcoReader _reader = new();

    private static byte[] PeFixtureBytes() => File.ReadAllBytes(TestFiles.PeFixture);

    /// <summary>Locates the group directory of the given type for the group with the given id.</summary>
    private static int GroupDirectoryOffset(byte[] pe, ResourceType groupType, uint groupId)
        => PeResources.DataOffset(pe, PeResources.Leaf(pe, groupType, groupId));

    /// <summary>Locates the RT_ICON or RT_CURSOR resource with the given id.</summary>
    private static int ImageResourceOffset(byte[] pe, ResourceType imageType, ushort resourceId)
        => PeResources.DataOffset(pe, PeResources.Leaf(pe, imageType, resourceId));

    private static int EntryOffset(int groupOffset, int entryIndex)
        => groupOffset + GroupHeaderSize + (entryIndex * GroupEntrySize) + ResourceIdField;

    private static ushort ResourceIdOfEntry(byte[] pe, int groupOffset, int entryIndex)
        => BitConverter.ToUInt16(pe, EntryOffset(groupOffset, entryIndex));

    private static void PointEntryAtAMissingResource(byte[] pe, int groupOffset, int entryIndex, ushort absentResourceId)
        => BitConverter.GetBytes(absentResourceId).CopyTo(pe, EntryOffset(groupOffset, entryIndex));

    /// <summary>
    /// Clears the start of a bitmap image, so that neither the BMP nor the PNG decoder recognises it
    /// and the image is read as a format no decoder supports.
    /// </summary>
    private static void MakeUnrecognisable(byte[] pe, int imageOffset)
    {
        Assert.Equal(BitmapInfoHeaderSize, BitConverter.ToInt32(pe, imageOffset));
        Array.Clear(pe, imageOffset, 8);
    }

    [Fact]
    public void Read_KeepsEveryEntryWhenAllResourcesArePresent()
    {
        // Positive control for the two cases below.
        var ico = _reader.Read(PeFixtureBytes());

        Assert.NotNull(ico);
        Assert.Equal(3, ico.GetIconGroup("2").Size);
    }

    [Fact]
    public void Read_DropsASingleEntryWhoseResourceIsMissing()
    {
        var pe = PeFixtureBytes();
        PointEntryAtAMissingResource(pe, GroupDirectoryOffset(pe, ResourceType.RT_GROUP_ICON, 2), entryIndex: 0, absentResourceId: 9999);

        var ico = _reader.Read(pe);

        Assert.NotNull(ico);
        var group = ico.GetIconGroup("2");

        Assert.Equal(2, group.Size);

        // The surviving entries must all have been resolved. Skipping one leaves it at 0, which the
        // entry count alone would not reveal.
        Assert.All(group.DirectoryEntries, entry => Assert.NotEqual(0u, entry.RealImageOffset));
    }

    [Fact]
    public void Read_DropsConsecutiveEntriesWhoseResourcesAreMissing()
    {
        // Removing from a list while indexing forward skips whatever shifts into the vacated slot,
        // so the second missing entry used to survive with an unresolved offset.
        var pe = PeFixtureBytes();
        var groupOffset = GroupDirectoryOffset(pe, ResourceType.RT_GROUP_ICON, 2);
        PointEntryAtAMissingResource(pe, groupOffset, entryIndex: 0, absentResourceId: 9999);
        PointEntryAtAMissingResource(pe, groupOffset, entryIndex: 1, absentResourceId: 9998);

        var ico = _reader.Read(pe);

        Assert.NotNull(ico);
        var group = ico.GetIconGroup("2");

        Assert.Equal(1, group.Size);
        Assert.All(group.DirectoryEntries, entry => Assert.NotEqual(0u, entry.RealImageOffset));
    }

    [Fact]
    public void Read_KeepsTheSurvivingEntryUsable()
    {
        var pe = PeFixtureBytes();
        var groupOffset = GroupDirectoryOffset(pe, ResourceType.RT_GROUP_ICON, 2);
        PointEntryAtAMissingResource(pe, groupOffset, entryIndex: 0, absentResourceId: 9999);
        PointEntryAtAMissingResource(pe, groupOffset, entryIndex: 1, absentResourceId: 9998);

        var ico = _reader.Read(pe);
        Assert.NotNull(ico);

        var group = ico.GetIconGroup("2");
        var image = PngImage.Parse(ico.GetImage(group, 0));

        Assert.Equal(ico.GetImageReference(group, 0).Width, image.Width);
    }

    [Fact]
    public void Read_KeepsEveryGroupNameWithItsGroupWhenAGroupHasNoLanguageEntry()
    {
        // A group whose directory lists no language holds no data and is dropped. The groups after it
        // must still carry their own names rather than the name of the group before them.
        var pe = PeFixtureBytes();
        var emptyGroup = PeResources.Leaf(pe, ResourceType.RT_GROUP_ICON, 2);
        PeResources.WriteUInt16(pe, emptyGroup.LanguageDirectoryOffset + LanguageCountField, 0);

        var ico = _reader.Read(pe);

        Assert.NotNull(ico);
        Assert.Equal([("1", 1), ("3", 1), ("4", 3)], ico.IconGroups.Select(x => (x.Name, x.Size)));
    }

    [Fact]
    public void Read_ResolvesCursorGroupsToCursorsWhenIconsShareTheirIds()
    {
        // Resource ids only have to be unique within their type. rc.exe numbers icons and cursors from
        // one counter, but the resource compiler behind tk86.dll numbers both from 1.
        var baseline = _reader.Read(PeFixtureBytes())!;
        var pe = PeFixtureBytes();
        PeResources.NumberCursorsFromOne(pe);

        Assert.Subset(
            PeResources.LeavesOf(pe, ResourceType.RT_ICON).Select(x => x.Id).ToHashSet(),
            PeResources.LeavesOf(pe, ResourceType.RT_CURSOR).Select(x => x.Id).ToHashSet());

        var ico = _reader.Read(pe);

        Assert.NotNull(ico);
        foreach (var expected in baseline.CursorGroups)
        {
            var actual = ico.GetCursorGroup(expected.Name);

            Assert.All(Enumerable.Range(0, actual.Size), i => Assert.Equal(IcoType.Cursor, ico.GetImageReference(actual, i).IcoType));
            Assert.Equal(
                expected.DirectoryEntries.Select(x => (x.Width, x.HotspotX, x.HotspotY)),
                actual.DirectoryEntries.Select(x => (x.Width, x.HotspotX, x.HotspotY)));
            Assert.Equal(
                Enumerable.Range(0, expected.Size).Select(i => baseline.GetImage(expected, i)),
                Enumerable.Range(0, actual.Size).Select(i => ico.GetImage(actual, i)));
        }
    }

    [Fact]
    public void Read_KeepsEveryOtherIconWhenOneIsInAnUnrecognisedFormat()
    {
        // An image no decoder recognises is skipped. It used to end the image loop, which also
        // skipped building every icon group.
        var baseline = _reader.Read(PeFixtureBytes())!;
        var pe = PeFixtureBytes();
        var groupOffset = GroupDirectoryOffset(pe, ResourceType.RT_GROUP_ICON, 2);
        MakeUnrecognisable(pe, ImageResourceOffset(pe, ResourceType.RT_ICON, ResourceIdOfEntry(pe, groupOffset, entryIndex: 1)));

        var ico = _reader.Read(pe);

        Assert.NotNull(ico);
        Assert.Equal(baseline.ImageReferences.Count - 1, ico.ImageReferences.Count);
        Assert.Equal(baseline.IconGroups.Select(x => x.Name), ico.IconGroups.Select(x => x.Name));
        Assert.Equal(baseline.CursorGroups.Select(x => x.Name), ico.CursorGroups.Select(x => x.Name));

        var expected = baseline.GetIconGroup("2");
        var group = ico.GetIconGroup("2");
        Assert.Equal(
            new[] { 0, 2 }.Select(i => baseline.GetImageReference(expected, i).Width),
            Enumerable.Range(0, group.Size).Select(i => PngImage.Parse(ico.GetImage(group, i)).Width));
    }

    [Fact]
    public void Read_KeepsEveryOtherCursorWhenOneIsInAnUnrecognisedFormat()
    {
        var baseline = _reader.Read(PeFixtureBytes())!;
        var pe = PeFixtureBytes();
        var groupOffset = GroupDirectoryOffset(pe, ResourceType.RT_GROUP_CURSOR, 2);
        var resourceOffset = ImageResourceOffset(pe, ResourceType.RT_CURSOR, ResourceIdOfEntry(pe, groupOffset, entryIndex: 1));

        // An RT_CURSOR resource carries its hotspot ahead of the image.
        MakeUnrecognisable(pe, resourceOffset + CursorHotspotSize);

        var ico = _reader.Read(pe);

        Assert.NotNull(ico);
        Assert.Equal(baseline.ImageReferences.Count - 1, ico.ImageReferences.Count);
        Assert.Equal(baseline.CursorGroups.Select(x => x.Name), ico.CursorGroups.Select(x => x.Name));
        Assert.Equal(baseline.IconGroups.Select(x => x.Name), ico.IconGroups.Select(x => x.Name));

        var expected = baseline.GetCursorGroup("2").DirectoryEntries!;
        Assert.Equal(
            new[] { expected[0], expected[2] }.Select(x => (x.Width, x.HotspotX, x.HotspotY)),
            ico.GetCursorGroup("2").DirectoryEntries!.Select(x => (x.Width, x.HotspotX, x.HotspotY)));
    }
}
