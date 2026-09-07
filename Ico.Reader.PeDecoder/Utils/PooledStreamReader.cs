using System.Buffers;

namespace Ico.Reader.PeDecoder.Utils;

internal delegate T SpanParser<out T>(ReadOnlySpan<byte> data);

internal static class PooledStreamReader
{
    /// <summary>
    /// Reads <paramref name="byteCount"/> bytes from the stream and hands them to
    /// <paramref name="parse"/>.
    /// </summary>
    /// <remarks>
    /// Every length in a PE file is attacker controlled, and a section table or resource directory
    /// can declare tens of thousands of entries. Stack-allocating that much is an uncatchable
    /// <see cref="StackOverflowException"/>, so the scratch buffer is always pooled.
    /// </remarks>
    public static T Read<T>(Stream stream, int byteCount, SpanParser<T> parse)
    {
        if (byteCount <= 0)
            return parse(ReadOnlySpan<byte>.Empty);

        var buffer = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            stream.Read(buffer, 0, byteCount);
            return parse(new ReadOnlySpan<byte>(buffer, 0, byteCount));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
