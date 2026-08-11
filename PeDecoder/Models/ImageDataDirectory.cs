using System.Runtime.InteropServices;

namespace PeDecoder.Models;

internal sealed class ImageDataDirectory
{
    public uint VirtualAddress { get; init; }
    public uint Size { get; init; }

    public SectionHeader FindFileSectionHeader(IEnumerable<SectionHeader> sectionHeaders)
    {
        foreach (var section in sectionHeaders)
        {
            if (VirtualAddress >= section.VirtualAddress && VirtualAddress < section.VirtualAddress + section.SizeOfRawData)
                return section;
        }

        throw new InvalidDataException("VirtualAddress is outside the range of the section headers.");
    }
}