namespace Ico.Reader.PeDecoder.Models;

// https://learn.microsoft.com/en-gb/windows/win32/debug/pe-format?redirectedfrom=MSDN#section-table-section-headers
internal sealed class SectionHeader
{
    public const uint SectionSize = 40;

    public string Name { get; init; } = null!;
    public uint VirtualSize { get; init; }
    public uint VirtualAddress { get; init; }
    public uint SizeOfRawData { get; init; }
    public uint PointerToRawData { get; init; }
    public uint PointerToRelocations { get; init; }
    public uint PointerToLinenumbers { get; init; }
    public ushort NumberOfRelocations { get; init; }
    public ushort NumberOfLinenumbers { get; init; }
    public SectionFlag Characteristics { get; init; }

    public override string ToString() => $"{Name}";

    public uint GetFileOffset(uint rva)
    {
        if (rva < VirtualAddress || rva >= VirtualAddress + SizeOfRawData)
            throw new ArgumentOutOfRangeException(nameof(rva), "RVA is outside the range of the section");

        return rva - VirtualAddress + PointerToRawData;
    }
}
