using System.Runtime.InteropServices;

using Ico.Reader.Data;

namespace Ico.Reader.Reading;

/// <summary>
/// Reads the six byte header that opens an ICO or CUR file, and each ico group inside a PE.
/// </summary>
internal static class IcoHeaderReader
{
    internal const int HeaderSize = 6;

    /// <summary>
    /// Reads an <see cref="IcoHeader"/> from a stream.
    /// <para>
    /// For more information, see <see href="https://en.wikipedia.org/wiki/ICO_(file_format)#Outline">Outline</see>.
    /// </para>
    /// </summary>
    /// <param name="stream">The input stream to read from.</param>
    /// <param name="startPosition">The position in the stream at which to begin reading.</param>
    public static IcoHeader Read(Stream stream, long startPosition = 0)
    {
        stream.Position = startPosition;

        Span<byte> buffer = stackalloc byte[HeaderSize];
        stream.ReadExactly(buffer);

        ReadOnlySpan<byte> header = buffer;

        return new IcoHeader
        {
            Reserved = MemoryMarshal.Read<ushort>(header.Slice(0, 2)),
            ImageType = MemoryMarshal.Read<ushort>(header.Slice(2, 2)),
            ImageCount = MemoryMarshal.Read<ushort>(header.Slice(4, 2))
        };
    }
}
