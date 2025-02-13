using System.Runtime.InteropServices;
using System.Text;

namespace PeDecoder.Models;
// https://learn.microsoft.com/en-gb/windows/win32/debug/pe-format?redirectedfrom=MSDN#section-table-section-headers
public class SectionHeader
{
    public const uint SectionSize = 40;

    public string Name { get; set; }
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

    public static SectionHeader[] ReadFromStream(Stream stream, PE_Header peHeader)
    {
        stream.Position = peHeader.SizeOfOptionalHeader + peHeader.HeaderOffset + PE_Header.PeHeaderSize;

        var sections = new SectionHeader[peHeader.NumberOfSections];

        var sectionsSize = peHeader.NumberOfSections * SectionSize;
        Span<byte> data = stackalloc byte[(int)sectionsSize];
        stream.Read(data);

        ReadOnlySpan<byte> readOnlyData = data;
        Span<char> nameChars = stackalloc char[8];

        for (int i = 0; i < peHeader.NumberOfSections; i++)
        {
            var sectionIndex = i * (int)SectionSize;

            sections[i] = new SectionHeader();

            Encoding.UTF8.GetChars(readOnlyData.Slice(sectionIndex, 8), nameChars);
            sections[i].Name = new string(nameChars).Trim('\0');

            sections[i].VirtualSize = MemoryMarshal.Read<uint>(readOnlyData.Slice(sectionIndex + 8, 4));
            sections[i].VirtualAddress = MemoryMarshal.Read<uint>(readOnlyData.Slice(sectionIndex + 12, 4));
            sections[i].SizeOfRawData = MemoryMarshal.Read<uint>(readOnlyData.Slice(sectionIndex + 16, 4));
            sections[i].PointerToRawData = MemoryMarshal.Read<uint>(readOnlyData.Slice(sectionIndex + 20, 4));
            sections[i].PointerToRelocations = MemoryMarshal.Read<uint>(readOnlyData.Slice(sectionIndex + 24, 4));
            sections[i].PointerToLinenumbers = MemoryMarshal.Read<uint>(readOnlyData.Slice(sectionIndex + 28, 4));
            sections[i].NumberOfRelocations = MemoryMarshal.Read<ushort>(readOnlyData.Slice(sectionIndex + 32, 2));
            sections[i].NumberOfLinenumbers = MemoryMarshal.Read<ushort>(readOnlyData.Slice(sectionIndex + 34, 2));
            sections[i].Characteristics = (SectionFlag)MemoryMarshal.Read<uint>(readOnlyData.Slice(sectionIndex + 36, 4));
        }

        return sections;
    }

    public uint GetFileOffset(uint rva)
    {
        if (rva < VirtualAddress || rva >= VirtualAddress + SizeOfRawData)
            throw new ArgumentOutOfRangeException(nameof(rva), "RVA is outside the range of the section");

        return rva - VirtualAddress + PointerToRawData;
    }
}
