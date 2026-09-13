using Ico.Reader.PeDecoder;
using Ico.Reader.PeDecoder.Models;

namespace Ico.Reader.Test.Integration;

/// <summary>
/// Unusual PE layouts that the Windows loader accepts. Reading them has to give the icons and cursors of the untouched
/// fixture, and a file that only starts like a PE has to read as null rather than throw.
/// </summary>
public sealed class PeLayoutToleranceTests
{
    private const int ReservedDirectoryIndex = 15;
    private const int DataEntryReservedField = 12;

    private readonly IcoReader _reader = new();

    private static byte[] PeFixtureBytes() => File.ReadAllBytes(TestFiles.PeFixture);

    /// <summary>An old 16-bit executable: the DOS header points at an NE header rather than a PE signature.</summary>
    private static byte[] NeExecutable()
    {
        var bytes = new byte[256];
        bytes[0] = (byte)'M';
        bytes[1] = (byte)'Z';
        BitConverter.GetBytes(64).CopyTo(bytes, 60);
        bytes[64] = (byte)'N';
        bytes[65] = (byte)'E';
        BitConverter.GetBytes((ushort)0x40).CopyTo(bytes, 64 + 20);

        return bytes;
    }

    private void AssertReadsLikeTheUntouchedFixture(byte[] pe)
    {
        var expected = _reader.Read(PeFixtureBytes())!;
        var actual = _reader.Read(pe);

        Assert.NotNull(actual);
        Assert.Equal(expected.Groups.Select(Describe), actual.Groups.Select(Describe));
        Assert.Equal(expected.ImageReferences.Count, actual.ImageReferences.Count);
    }

    private static (IcoType, string, int) Describe(IIcoGroup group) => (group.IcoType, group.Name, group.Size);

    [Fact]
    public void Read_AcceptsAnOptionalHeaderWithFifteenDataDirectories()
    {
        var pe = PeFixtureBytes();
        var original = (byte[])pe.Clone();
        var sectionTable = PeResources.SectionTableOffset(pe);
        var sectionTableSize = PeResources.SectionCount(pe) * PeResources.SectionHeaderSize;
        var sizeOfOptionalHeader = BitConverter.ToUInt16(pe, PeResources.SizeOfOptionalHeaderOffset(pe));

        PeResources.WriteUInt32(pe, PeResources.NumberOfRvaAndSizesOffset(pe), 15);
        PeResources.WriteUInt16(pe, PeResources.SizeOfOptionalHeaderOffset(pe), (ushort)(sizeOfOptionalHeader - PeResources.DataDirectorySize));
        Buffer.BlockCopy(original, sectionTable, pe, sectionTable - PeResources.DataDirectorySize, sectionTableSize);
        Array.Clear(pe, sectionTable - PeResources.DataDirectorySize + sectionTableSize, PeResources.DataDirectorySize);

        AssertReadsLikeTheUntouchedFixture(pe);
    }

    [Fact]
    public void Read_IgnoresAReservedDataDirectoryThatIsNotEmpty()
    {
        var pe = PeFixtureBytes();
        PeResources.WriteUInt32(pe, PeResources.DataDirectoryOffset(pe, ReservedDirectoryIndex), 0x1234);

        AssertReadsLikeTheUntouchedFixture(pe);
    }

    [Fact]
    public void Read_IgnoresTheReservedFieldOfAResourceDataEntry()
    {
        var pe = PeFixtureBytes();
        var leaf = PeResources.LeavesOf(pe, ResourceType.RT_ICON).First();
        PeResources.WriteUInt32(pe, leaf.DataEntryOffset + DataEntryReservedField, 1);

        AssertReadsLikeTheUntouchedFixture(pe);
    }

    [Fact]
    public void Read_SkipsAGroupWhoseHeaderDeclaresTheOtherImageType()
    {
        var baseline = _reader.Read(PeFixtureBytes())!;
        var pe = PeFixtureBytes();
        var group = PeResources.DataOffset(pe, PeResources.Leaf(pe, ResourceType.RT_GROUP_ICON, 2));
        PeResources.WriteUInt16(pe, group + 2, CursorDirectoryEntry.ImageType);

        var ico = _reader.Read(pe);

        Assert.NotNull(ico);
        Assert.Equal(["1", "3", "4"], ico.IconGroups.Select(x => x.Name));
        Assert.Equal(baseline.CursorGroups.Select(Describe), ico.CursorGroups.Select(Describe));
    }

    /// <summary>
    /// The offsets inside a resource tree count from its root. Only when the tree starts its section, as it usually
    /// does, do they coincide with offsets from the section start.
    /// </summary>
    [Fact]
    public void Read_ResolvesTheResourceTreeFromItsRootWhenTheTreeDoesNotStartItsSection()
    {
        const int Lead = 16;
        var expected = _reader.Read(PeFixtureBytes())!;
        var pe = PeFixtureBytes();
        var section = PeResources.SectionOf(pe, BitConverter.ToUInt32(pe, PeResources.ResourceTableOffset(pe)));
        PeResources.WriteUInt32(pe, section.HeaderOffset + 8, section.VirtualSize + Lead);
        PeResources.WriteUInt32(pe, section.HeaderOffset + 12, section.VirtualAddress - Lead);
        PeResources.WriteUInt32(pe, section.HeaderOffset + 16, section.SizeOfRawData + Lead);
        PeResources.WriteUInt32(pe, section.HeaderOffset + 20, section.PointerToRawData - Lead);

        var actual = _reader.Read(pe);

        Assert.NotNull(actual);
        Assert.Equal(expected.Groups.Select(Describe), actual.Groups.Select(Describe));
        Assert.Equal(expected.ImageReferences.Select(expected.GetImage), actual.ImageReferences.Select(actual.GetImage));
    }

    /// <summary>
    /// UPX compresses every icon outside the first icon group, leaving their data entries pointing at memory the
    /// unpacker only fills at run time. Windows still loads the first group of such a file.
    /// </summary>
    [Fact]
    public void Read_KeepsTheGroupsWhoseImagesAreStoredInTheFile()
    {
        var baseline = _reader.Read(PeFixtureBytes())!;
        var pe = PeFixtureBytes();
        var firstGroupImages = PeResources.GroupResourceIds(pe, PeResources.Leaf(pe, ResourceType.RT_GROUP_ICON, 1));

        foreach (var leaf in PeResources.LeavesOf(pe, ResourceType.RT_ICON).Where(x => !firstGroupImages.Contains((ushort)x.Id)))
            PeResources.WriteUInt32(pe, leaf.DataEntryOffset, 0x7FFF_0000);

        var ico = _reader.Read(pe);

        Assert.NotNull(ico);
        Assert.Equal(["1"], ico.IconGroups.Select(x => x.Name));
        Assert.Equal(baseline.GetImage(baseline.GetIconGroup("1"), 0), ico.GetImage(ico.GetIconGroup("1"), 0));
        Assert.Equal(baseline.CursorGroups.Select(Describe), ico.CursorGroups.Select(Describe));
    }

    [Fact]
    public void Read_ReturnsNullForAnMzFileThatIsNotAPe() => Assert.Null(_reader.Read(NeExecutable()));

    [Fact]
    public void IsPeFormat_RequiresThePeSignature()
    {
        var decoder = new PeFileDecoder();

        Assert.True(decoder.IsPeFormat(new MemoryStream(PeFixtureBytes())));
        Assert.False(decoder.IsPeFormat(new MemoryStream(NeExecutable())));
    }

    [Fact]
    public void DecodePE_ThrowsWithoutThePeSignature()
        => Assert.Throws<InvalidDataException>(() => new PeFileDecoder().DecodePE(new MemoryStream(NeExecutable())));
}
