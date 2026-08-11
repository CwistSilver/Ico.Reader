using PeDecoder.Models;
using PeDecoder.Reading;

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

    private readonly IcoReader _reader = new();

    private static byte[] PeFixtureBytes() => File.ReadAllBytes(TestFiles.PeFixture);

    /// <summary>Locates the RT_GROUP_ICON directory for the group with the given name.</summary>
    private static int GroupIconDirectoryOffset(byte[] pe, string groupName)
    {
        using var stream = new MemoryStream(pe);
        var peHeader = new global::PeDecoder.PeDecoder().DecodePE(stream);
        var root = ResourceReader.Read(stream, peHeader)!;

        var groupDirectory = root.GetDirectory(ResourceType.RT_GROUP_ICON.ToString())!;
        var index = groupDirectory.Subdirectories.FindIndex(x => x.Name == groupName);

        return (int)root.GetResources(ResourceType.RT_GROUP_ICON.ToString())![index].GetFileOffset(root.Section!);
    }

    private static void PointEntryAtAMissingResource(byte[] pe, int groupOffset, int entryIndex, ushort absentResourceId)
        => BitConverter.GetBytes(absentResourceId).CopyTo(pe, groupOffset + GroupHeaderSize + (entryIndex * GroupEntrySize) + ResourceIdField);

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
        PointEntryAtAMissingResource(pe, GroupIconDirectoryOffset(pe, "2"), entryIndex: 0, absentResourceId: 9999);

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
        var groupOffset = GroupIconDirectoryOffset(pe, "2");
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
        var groupOffset = GroupIconDirectoryOffset(pe, "2");
        PointEntryAtAMissingResource(pe, groupOffset, entryIndex: 0, absentResourceId: 9999);
        PointEntryAtAMissingResource(pe, groupOffset, entryIndex: 1, absentResourceId: 9998);

        var ico = _reader.Read(pe);
        Assert.NotNull(ico);

        var group = ico.GetIconGroup("2");
        var image = PngImage.Parse(ico.GetImage(group, 0));

        Assert.Equal(ico.GetImageReference(group, 0).Width, image.Width);
    }
}
