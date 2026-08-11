using System.Runtime.InteropServices;
using System.Text;

using PeDecoder.Utils;

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

    public static SectionHeader[] ReadFromStream(Stream stream, PeHeader peHeader)
    {
        stream.Position = peHeader.SizeOfOptionalHeader + peHeader.HeaderOffset + PeHeader.PeHeaderSize;

        var sectionCount = peHeader.NumberOfSections;

        return PooledStreamReader.Read(stream, (int)(sectionCount * SectionSize), data =>
        {
            var sections = new SectionHeader[sectionCount];
            Span<char> nameChars = stackalloc char[8];

            for (var i = 0; i < sectionCount; i++)
            {
                var sectionIndex = i * (int)SectionSize;
                Encoding.UTF8.GetChars(data.Slice(sectionIndex, 8), nameChars);

                sections[i] = new SectionHeader
                {
                    // A section name is eight bytes, null-padded when shorter.
                    Name = nameChars.ToString().Trim('\0'),
                    VirtualSize = MemoryMarshal.Read<uint>(data.Slice(sectionIndex + 8, 4)),
                    VirtualAddress = MemoryMarshal.Read<uint>(data.Slice(sectionIndex + 12, 4)),
                    SizeOfRawData = MemoryMarshal.Read<uint>(data.Slice(sectionIndex + 16, 4)),
                    PointerToRawData = MemoryMarshal.Read<uint>(data.Slice(sectionIndex + 20, 4)),
                    PointerToRelocations = MemoryMarshal.Read<uint>(data.Slice(sectionIndex + 24, 4)),
                    PointerToLinenumbers = MemoryMarshal.Read<uint>(data.Slice(sectionIndex + 28, 4)),
                    NumberOfRelocations = MemoryMarshal.Read<ushort>(data.Slice(sectionIndex + 32, 2)),
                    NumberOfLinenumbers = MemoryMarshal.Read<ushort>(data.Slice(sectionIndex + 34, 2)),
                    Characteristics = (SectionFlag)MemoryMarshal.Read<uint>(data.Slice(sectionIndex + 36, 4))
                };
            }

            return sections;
        });
    }

    public uint GetFileOffset(uint rva)
    {
        if (rva < VirtualAddress || rva >= VirtualAddress + SizeOfRawData)
            throw new ArgumentOutOfRangeException(nameof(rva), "RVA is outside the range of the section");

        return rva - VirtualAddress + PointerToRawData;
    }
}
