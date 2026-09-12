#if NETFRAMEWORK

namespace System.IO.Compression;

/// <summary>
/// Decompression-only stand-in for the <c>ZLibStream</c> that .NET Framework lacks. Strips the
/// two byte zlib header and inflates the rest; the trailing Adler-32 checksum is not verified.
/// </summary>
internal sealed class ZLibStream : Stream
{
    private readonly DeflateStream _deflate;

    public ZLibStream(Stream stream, CompressionMode mode)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));
        if (mode != CompressionMode.Decompress)
            throw new NotSupportedException("Only decompression is supported.");

        SkipHeader(stream);
        _deflate = new DeflateStream(stream, CompressionMode.Decompress);
    }

    public override bool CanRead => _deflate.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() => _deflate.Flush();

    public override int Read(byte[] buffer, int offset, int count) => _deflate.Read(buffer, offset, count);

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _deflate.Dispose();

        base.Dispose(disposing);
    }

    private static void SkipHeader(Stream stream)
    {
        var cmf = stream.ReadByte();
        var flg = stream.ReadByte();

        if (cmf < 0 || flg < 0)
            throw new InvalidDataException("The stream ended before the zlib header was complete.");
        if ((cmf & 0x0F) != 8)
            throw new InvalidDataException($"Expected the deflate compression method, got {cmf & 0x0F}.");
        if (((cmf << 8) + flg) % 31 != 0)
            throw new InvalidDataException("The zlib header check bits are wrong.");
    }
}

#endif
