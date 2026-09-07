namespace Ico.Reader.PeDecoder.Models;

/// <summary>
/// The DOS header every PE file still begins with. Only the signature matters here; the remaining
/// fields are kept because they describe the format.
/// </summary>
internal sealed class MzHeader
{
    internal const int HeaderSize = 28;

    public required bool HasMzSignature { get; init; }
    public required ushort BytesInLastBlock { get; init; }
    public required ushort BlocksInFile { get; init; }
    public required ushort NumRelocs { get; init; }
    public required ushort HeaderParagraphs { get; init; }
    public required ushort MinExtraParagraphs { get; init; }
    public required ushort MaxExtraParagraphs { get; init; }
    public required ushort Ss { get; init; }
    public required ushort Sp { get; init; }
    public required ushort Checksum { get; init; }
    public required ushort Ip { get; init; }
    public required ushort Cs { get; init; }
    public required ushort RelocTableOffset { get; init; }
    public required ushort OverlayNumber { get; init; }
}
