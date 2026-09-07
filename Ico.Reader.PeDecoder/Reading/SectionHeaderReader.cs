using System.Runtime.InteropServices;
using System.Text;

using Ico.Reader.PeDecoder.Models;
using Ico.Reader.PeDecoder.Utils;

namespace Ico.Reader.PeDecoder.Reading;

/// <summary>
/// Reads the section table that maps virtual addresses onto file offsets.
/// </summary>
internal static class SectionHeaderReader
{
    public static SectionHeader[] Read(Stream stream, PeHeader peHeader)
    {
        stream.Position = peHeader.SizeOfOptionalHeader + peHeader.HeaderOffset + PeHeader.PeHeaderSize;

        var sectionCount = peHeader.NumberOfSections;

        return PooledStreamReader.Read(stream, (int)(sectionCount * SectionHeader.SectionSize), data =>
        {
            var sections = new SectionHeader[sectionCount];
            Span<char> nameChars = stackalloc char[8];

            for (var i = 0; i < sectionCount; i++)
            {
                var sectionIndex = i * (int)SectionHeader.SectionSize;
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
}
