using System.Runtime.InteropServices;

namespace Ico.Reader.PeDecoder.Models;

/// <summary>
/// Locates one of the tables the optional header points at, such as the resource table.
/// </summary>
public sealed class ImageDataDirectory
{
    /// <summary>
    /// Address of the table relative to the image base once loaded.
    /// </summary>
    public uint VirtualAddress { get; init; }

    /// <summary>
    /// Size of the table in bytes.
    /// </summary>
    public uint Size { get; init; }

    /// <summary>
    /// Finds the section whose virtual address range contains this directory.
    /// </summary>
    /// <param name="sectionHeaders">The sections of the image to search.</param>
    /// <returns>The section holding this directory.</returns>
    /// <exception cref="InvalidDataException">Thrown if no section covers <see cref="VirtualAddress"/>.</exception>
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
