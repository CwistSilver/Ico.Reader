namespace Ico.Reader.PeDecoder.Models;

/// <summary>
/// The COFF header that follows the PE signature, together with the optional header when the file
/// carries one.
/// </summary>
public sealed class PeHeader
{
    /// <summary>
    /// Size in bytes of the PE signature and the COFF header that follows it.
    /// </summary>
    public const uint PeHeaderSize = 24;

    /// <summary>
    /// The machine architecture the image targets.
    /// </summary>
    public MachineType Machine { get; init; }

    /// <summary>
    /// Number of entries in the section table.
    /// </summary>
    public ushort NumberOfSections { get; init; }

    /// <summary>
    /// When the file was created, as recorded by the linker.
    /// </summary>
    public DateTime TimeDateStamp { get; init; }

    /// <summary>
    /// File offset of the COFF symbol table, or zero when there is none.
    /// </summary>
    public uint PointerToSymbolTable { get; init; }

    /// <summary>
    /// Number of entries in the COFF symbol table.
    /// </summary>
    public uint NumberOfSymbols { get; init; }

    /// <summary>
    /// Size in bytes of the optional header. Zero for object files.
    /// </summary>
    public ushort SizeOfOptionalHeader { get; init; }

    /// <summary>
    /// Attributes of the image, such as whether it is a DLL.
    /// </summary>
    public Characteristics Characteristics { get; init; }

    /// <summary>
    /// File offset the PE signature was found at.
    /// </summary>
    public uint HeaderOffset { get; init; }

    /// <summary>
    /// The optional header, or <see langword="null"/> when the file does not carry one.
    /// </summary>
    public OptionalHeader? Optional { get; init; }
}
