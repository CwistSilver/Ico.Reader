using System.Buffers.Binary;

namespace Ico.Reader.Extensions;
internal static class BinaryWriterExtensions
{
    internal static void WriteUInt32BigEndian(this BinaryWriter writer, uint value)
    {
        Span<byte> span = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(span, value);
        writer.Write(span);
    }

    internal static void WriteInt32BigEndian(this BinaryWriter writer, int value)
    {
        Span<byte> span = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(span, value);
        writer.Write(span);
    }
}

