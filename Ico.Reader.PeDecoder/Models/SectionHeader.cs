namespace Ico.Reader.PeDecoder.Models;

/// <summary>
/// One entry of the section table, describing where a section lives in the file and in memory.
/// </summary>
/// <remarks>
/// See <see href="https://learn.microsoft.com/en-gb/windows/win32/debug/pe-format#section-table-section-headers"/>.
/// </remarks>
public sealed class SectionHeader
{
    /// <summary>
    /// Size in bytes of one section header.
    /// </summary>
    public const uint SectionSize = 40;

    /// <summary>
    /// The section name, such as <c>.rsrc</c>.
    /// </summary>
    public string Name { get; init; } = null!;

    /// <summary>
    /// Size of the section once loaded. May exceed <see cref="SizeOfRawData"/>, in which case the remainder is zero filled.
    /// </summary>
    public uint VirtualSize { get; init; }

    /// <summary>
    /// Address of the section relative to the image base once loaded.
    /// </summary>
    public uint VirtualAddress { get; init; }

    /// <summary>
    /// Size of the section as stored in the file, rounded up to the file alignment.
    /// </summary>
    public uint SizeOfRawData { get; init; }

    /// <summary>
    /// File offset of the section contents.
    /// </summary>
    public uint PointerToRawData { get; init; }

    /// <summary>
    /// File offset of the section relocations. Zero for images.
    /// </summary>
    public uint PointerToRelocations { get; init; }

    /// <summary>
    /// File offset of the line number entries. Zero when there are none.
    /// </summary>
    public uint PointerToLinenumbers { get; init; }

    /// <summary>
    /// Number of relocation entries. Zero for images.
    /// </summary>
    public ushort NumberOfRelocations { get; init; }

    /// <summary>
    /// Number of line number entries. Zero when there are none.
    /// </summary>
    public ushort NumberOfLinenumbers { get; init; }

    /// <summary>
    /// Attributes of the section, describing what it holds and how it is mapped.
    /// </summary>
    public SectionFlag Characteristics { get; init; }

    /// <inheritdoc />
    public override string ToString() => $"{Name}";

    /// <summary>
    /// Translates an address relative to the image base into an offset within the file.
    /// </summary>
    /// <param name="rva">The address to translate, which must fall inside this section.</param>
    /// <returns>The corresponding file offset.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="rva"/> lies outside this section.</exception>
    public uint GetFileOffset(uint rva)
    {
        if (rva < VirtualAddress || rva >= VirtualAddress + SizeOfRawData)
            throw new ArgumentOutOfRangeException(nameof(rva), "RVA is outside the range of the section");

        return rva - VirtualAddress + PointerToRawData;
    }

    /// <summary>
    /// Translates the address of <paramref name="size"/> bytes into an offset within the file, provided the raw data of
    /// this section holds all of them.
    /// </summary>
    internal bool TryGetFileOffset(uint rva, uint size, out uint fileOffset)
    {
        fileOffset = 0;

        var rawDataEnd = (ulong)VirtualAddress + SizeOfRawData;
        if (rva < VirtualAddress || (ulong)rva + size > rawDataEnd)
            return false;

        var offset = (ulong)rva - VirtualAddress + PointerToRawData;
        if (offset > uint.MaxValue)
            return false;

        fileOffset = (uint)offset;
        return true;
    }
}
