using Ico.Reader.PeDecoder.Models;

namespace Ico.Reader.Test.Unit.PeDecoder;

/// <summary>
/// A resource's address is only worth reading when the raw data of a section holds all of it. A packer can leave data
/// entries pointing at memory it only fills at run time, in a section that has no raw data at all.
/// </summary>
public sealed class ResourceDataEntryTests
{
    private static readonly SectionHeader[] _sections =
    [
        new() { Name = ".text", VirtualAddress = 0x1000, VirtualSize = 0x180, SizeOfRawData = 0x200, PointerToRawData = 0x400 },
        new() { Name = ".rsrc", VirtualAddress = 0x2000, VirtualSize = 0x3F0, SizeOfRawData = 0x400, PointerToRawData = 0x600 },
        new() { Name = "UPX0", VirtualAddress = 0x3000, VirtualSize = 0x1000, SizeOfRawData = 0, PointerToRawData = 0xA00 },
    ];

    [Fact]
    public void TryGetFileOffset_ResolvesDataInAnySection()
    {
        var entry = new ResourceDataEntry { DataRVA = 0x1010, Size = 0x20 };

        Assert.True(entry.TryGetFileOffset(_sections, out var fileOffset));
        Assert.Equal(0x410u, fileOffset);
    }

    [Fact]
    public void TryGetFileOffset_ResolvesDataEndingExactlyWhereTheRawDataEnds()
    {
        var entry = new ResourceDataEntry { DataRVA = 0x2300, Size = 0x100 };

        Assert.True(entry.TryGetFileOffset(_sections, out var fileOffset));
        Assert.Equal(0x900u, fileOffset);
    }

    [Fact]
    public void TryGetFileOffset_RejectsDataRunningPastTheRawData()
        => Assert.False(new ResourceDataEntry { DataRVA = 0x2300, Size = 0x101 }.TryGetFileOffset(_sections, out _));

    [Fact]
    public void TryGetFileOffset_RejectsDataInASectionWithoutRawData()
        => Assert.False(new ResourceDataEntry { DataRVA = 0x3010, Size = 0x10 }.TryGetFileOffset(_sections, out _));

    [Fact]
    public void TryGetFileOffset_RejectsAnAddressNoSectionCovers()
        => Assert.False(new ResourceDataEntry { DataRVA = 0x9000, Size = 0x10 }.TryGetFileOffset(_sections, out _));

    [Fact]
    public void TryGetFileOffset_RejectsDataWhoseEndOverflowsTheAddressSpace()
        => Assert.False(new ResourceDataEntry { DataRVA = 0x2000, Size = uint.MaxValue }.TryGetFileOffset(_sections, out _));
}
