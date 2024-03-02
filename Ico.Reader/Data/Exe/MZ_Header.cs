using System.Buffers.Binary;

namespace Ico.Reader.Data.Exe;
internal struct MZ_Header
{
    internal char[] Signature;
    internal ushort BytesInLastBlock;
    internal ushort BlocksInFile;
    internal ushort NumRelocs;
    internal ushort HeaderParagraphs;
    internal ushort MinExtraParagraphs;
    internal ushort MaxExtraParagraphs;
    internal ushort Ss;
    internal ushort Sp;
    internal ushort Checksum;
    internal ushort Ip;
    internal ushort Cs;
    internal ushort RelocTableOffset;
    internal ushort OverlayNumber;

    internal static MZ_Header ReadFromStream(Stream stream)
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
