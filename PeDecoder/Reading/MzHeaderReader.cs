using System.Buffers.Binary;

using PeDecoder.Models;

namespace PeDecoder.Reading;

/// <summary>
/// Reads the DOS header that every PE file still begins with.
/// </summary>
internal static class MzHeaderReader
{
    public static MzHeader Read(Stream stream)
    {
        stream.Position = 0;

        Span<byte> data = stackalloc byte[MzHeader.HeaderSize];
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
