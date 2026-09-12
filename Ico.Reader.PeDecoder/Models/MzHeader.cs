namespace Ico.Reader.PeDecoder.Models;

/// <summary>
/// The DOS header every PE file still begins with. Only the signature matters here; the remaining
/// fields are kept because they describe the format.
/// </summary>
public sealed class MzHeader
{
    internal const int HeaderSize = 28;

    /// <summary>
    /// Whether the file starts with the "MZ" signature, which every PE file carries.
    /// </summary>
    public required bool HasMzSignature { get; init; }

    /// <summary>
    /// Bytes used in the last 512 byte block of the DOS stub.
    /// </summary>
    public required ushort BytesInLastBlock { get; init; }

    /// <summary>
    /// Number of 512 byte blocks the DOS stub occupies.
    /// </summary>
    public required ushort BlocksInFile { get; init; }

    /// <summary>
    /// Number of entries in the DOS relocation table.
    /// </summary>
    public required ushort NumRelocs { get; init; }

    /// <summary>
    /// Size of the DOS header in 16 byte paragraphs.
    /// </summary>
    public required ushort HeaderParagraphs { get; init; }

    /// <summary>
    /// Minimum extra paragraphs the DOS stub needs beyond its own image.
    /// </summary>
    public required ushort MinExtraParagraphs { get; init; }

    /// <summary>
    /// Maximum extra paragraphs the DOS stub can use.
    /// </summary>
    public required ushort MaxExtraParagraphs { get; init; }

    /// <summary>
    /// Initial value of the DOS stack segment register, relative to the load address.
    /// </summary>
    public required ushort Ss { get; init; }

    /// <summary>
    /// Initial value of the DOS stack pointer register.
    /// </summary>
    public required ushort Sp { get; init; }

    /// <summary>
    /// Checksum of the DOS stub. Usually zero and not verified.
    /// </summary>
    public required ushort Checksum { get; init; }

    /// <summary>
    /// Initial value of the DOS instruction pointer register.
    /// </summary>
    public required ushort Ip { get; init; }

    /// <summary>
    /// Initial value of the DOS code segment register, relative to the load address.
    /// </summary>
    public required ushort Cs { get; init; }

    /// <summary>
    /// Offset of the DOS relocation table from the start of the file.
    /// </summary>
    public required ushort RelocTableOffset { get; init; }

    /// <summary>
    /// Overlay number. Zero for the main executable.
    /// </summary>
    public required ushort OverlayNumber { get; init; }
}
