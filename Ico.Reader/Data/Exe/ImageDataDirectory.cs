using System.Runtime.InteropServices;

namespace Ico.Reader.Data.Exe;
internal class ImageDataDirectory
{
    internal uint VirtualAddress { get; set; }
    internal uint Size { get; set; }

    internal static ImageDataDirectory ReadFromSpan(ReadOnlySpan<byte> optionalHeaderSpan, int offset)
    {
        return new ImageDataDirectory()
        {
            VirtualAddress = MemoryMarshal.Read<uint>(optionalHeaderSpan.Slice(offset, 4)),
            Size = MemoryMarshal.Read<uint>(optionalHeaderSpan.Slice(offset + 4, 4))
        };
    }

    internal SectionHeader FindFileSectionHeader(IEnumerable<SectionHeader> sectionHeaders)
    {
        foreach (var section in sectionHeaders)
        {
            if (VirtualAddress >= section.VirtualAddress && VirtualAddress < section.VirtualAddress + section.SizeOfRawData)
                return section;
        }

        throw new Exception("VirtualAddress is outside the range of the section headers");
    }
}