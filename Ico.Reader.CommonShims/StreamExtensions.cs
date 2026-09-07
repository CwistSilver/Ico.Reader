using System.Buffers;

namespace Ico.Reader.CommonShims;

internal static class StreamExtensions
{
    public static int Read(this Stream stream, Span<byte> buffer)
    {
        var sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
        try
        {
            var numRead = stream.Read(sharedBuffer, 0, buffer.Length);
            if ((uint)numRead > (uint)buffer.Length)
                throw new IOException("IOStream is too long.");

            new ReadOnlySpan<byte>(sharedBuffer, 0, numRead).CopyTo(buffer);
            return numRead;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(sharedBuffer);
        }
    }

    /// <summary>
    /// Fills <paramref name="buffer"/> from the stream.
    /// </summary>
    /// <exception cref="EndOfStreamException">The stream ended before the buffer was filled.</exception>
    public static void ReadExactly(this Stream stream, Span<byte> buffer)
    {
        if (buffer.Length == 0)
            return;

        var sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
        try
        {
            stream.ReadExactly(sharedBuffer, 0, buffer.Length);
            new ReadOnlySpan<byte>(sharedBuffer, 0, buffer.Length).CopyTo(buffer);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(sharedBuffer);
        }
    }

    /// <summary>
    /// Reads exactly <paramref name="count"/> bytes into <paramref name="buffer"/>.
    /// </summary>
    /// <exception cref="EndOfStreamException">The stream ended before <paramref name="count"/> bytes were read.</exception>
    public static void ReadExactly(this Stream stream, byte[] buffer, int offset, int count)
    {
        var totalRead = 0;
        while (totalRead < count)
        {
            var read = stream.Read(buffer, offset + totalRead, count - totalRead);
            if (read == 0)
                throw new EndOfStreamException($"Expected {count} bytes but the stream ended after {totalRead}.");

            totalRead += read;
        }
    }

    /// <summary>
    /// Reads exactly <paramref name="count"/> bytes into <paramref name="buffer"/>.
    /// </summary>
    /// <exception cref="EndOfStreamException">The stream ended before <paramref name="count"/> bytes were read.</exception>
    public static async Task ReadExactlyAsync(this Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var totalRead = 0;
        while (totalRead < count)
        {
            var read = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead, cancellationToken).ConfigureAwait(false);
            if (read == 0)
                throw new EndOfStreamException($"Expected {count} bytes but the stream ended after {totalRead}.");

            totalRead += read;
        }
    }

    public static void Write(this Stream stream, ReadOnlySpan<byte> buffer)
    {
        var sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
        try
        {
            buffer.CopyTo(sharedBuffer);
            stream.Write(sharedBuffer, 0, buffer.Length);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(sharedBuffer);
        }
    }
}
