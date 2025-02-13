using System.Buffers.Binary;

namespace PeDecoder.Models;
public struct MZ_Header
{
    public char[] Signature;
    public ushort BytesInLastBlock;
    public ushort BlocksInFile;
    public ushort NumRelocs;
    public ushort HeaderParagraphs;
    public ushort MinExtraParagraphs;
    public ushort MaxExtraParagraphs;
    public ushort Ss;
    public ushort Sp;
    public ushort Checksum;
    public ushort Ip;
    public ushort Cs;
    public ushort RelocTableOffset;
    public ushort OverlayNumber;

    public static MZ_Header ReadFromStream(Stream stream)
    {
        stream.Position = 0;

        var size = 28;
        Span<byte> data = stackalloc byte[size];
        stream.Read(data);

        ReadOnlySpan<byte> readOnlyData = data;

        var header = new MZ_Header
        {
            Signature = new char[] { (char)readOnlyData[0], (char)readOnlyData[1] },
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

        return header;
    }
}
