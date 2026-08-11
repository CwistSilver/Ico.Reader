namespace PeDecoder.Models;

// https://learn.microsoft.com/en-gb/windows/win32/debug/pe-format?redirectedfrom=MSDN#section-table-section-headers
internal sealed class SectionHeader
{
    public const uint SectionSize = 40;

    public string Name { get; set; } = null!;
    public uint VirtualSize { get; set; }
    public uint VirtualAddress { get; set; }
    public uint SizeOfRawData { get; set; }
    public uint PointerToRawData { get; set; }
    public uint PointerToRelocations { get; set; }
    public uint PointerToLinenumbers { get; set; }
    public ushort NumberOfRelocations { get; set; }
    public ushort NumberOfLinenumbers { get; set; }
    public SectionFlag Characteristics { get; set; }

    public override string ToString() => $"{Name}";

    public uint GetFileOffset(uint rva)
    {
        if (rva < VirtualAddress || rva >= VirtualAddress + SizeOfRawData)
            throw new ArgumentOutOfRangeException(nameof(rva), "RVA is outside the range of the section");

        return rva - VirtualAddress + PointerToRawData;
    }
}
