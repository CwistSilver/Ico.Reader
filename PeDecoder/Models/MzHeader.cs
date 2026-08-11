using System.Buffers.Binary;

namespace PeDecoder.Models;

/// <summary>
/// The DOS header every PE file still begins with. Only the signature matters here; the remaining
/// fields are kept because they describe the format.
/// </summary>
internal sealed class MzHeader
{
    private const int HeaderSize = 28;

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

    public static MzHeader ReadFromStream(Stream stream)
    {
        stream.Position = 0;

        Span<byte> data = stackalloc byte[HeaderSize];
        stream.Read(data);

        ReadOnlySpan<byte> readOnlyData = data;

        return new MzHeader
        {
            HasMzSignature = readOnlyData[0] == (byte)'M' && readOnlyData[1] == (byte)'Z',
            BytesInLastBlock = BinaryPrimitives.ReadUInt16LittleEndian(readOnlyData.Slice(2, 2)),
            BlocksInFile = BinaryPrimitives.ReadUInt16LittleEndian(readOnlyData.Slice(4, 2)),
            NumRelocs = BinaryPrimitives.ReadUInt16LittleEndian(readOnlyData.Slice(6, 2)),
            HeaderParagraphs = BinaryPrimitives.ReadUInt16LittleEndian(readOnlyData.Slice(8, 2)),
            MinExtraParagraphs = BinaryPrimitives.ReadUInt16LittleEndian(readOnlyData.Slice(10, 2)),
            MaxExtraParagraphs = BinaryPrimitives.ReadUInt16LittleEndian(readOnlyData.Slice(12, 2)),
            Ss = BinaryPrimitives.ReadUInt16LittleEndian(readOnlyData.Slice(14, 2)),
            Sp = BinaryPrimitives.ReadUInt16LittleEndian(readOnlyData.Slice(16, 2)),
            Checksum = BinaryPrimitives.ReadUInt16LittleEndian(readOnlyData.Slice(18, 2)),
            Ip = BinaryPrimitives.ReadUInt16LittleEndian(readOnlyData.Slice(20, 2)),
            Cs = BinaryPrimitives.ReadUInt16LittleEndian(readOnlyData.Slice(22, 2)),
            RelocTableOffset = BinaryPrimitives.ReadUInt16LittleEndian(readOnlyData.Slice(24, 2)),
            OverlayNumber = BinaryPrimitives.ReadUInt16LittleEndian(readOnlyData.Slice(26, 2))
        };
    }
}
