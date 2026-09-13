namespace Ico.Reader.Data.Source;

/// <summary>
/// A read-only view of a stream the library does not own. Position 0 of the view is where the stream stood when it was
/// handed over, and disposing the view leaves the stream open.
/// </summary>
internal sealed class StreamWindow : Stream
{
    private readonly Stream _inner;
    private readonly long _origin;

    public StreamWindow(Stream inner, long origin)
    {
        _inner = inner;
        _origin = origin;
    }

    public override bool CanRead => _inner.CanRead;
    public override bool CanSeek => _inner.CanSeek;
    public override bool CanWrite => false;
    public override long Length => _inner.Length - _origin;

    public override long Position
    {
        get => _inner.Position - _origin;
        set => _inner.Position = _origin + value;
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => _inner.ReadAsync(buffer, offset, count, cancellationToken);

    public override long Seek(long offset, SeekOrigin origin) => origin switch
    {
        SeekOrigin.Begin => _inner.Seek(_origin + offset, SeekOrigin.Begin) - _origin,
        SeekOrigin.Current => _inner.Seek(offset, SeekOrigin.Current) - _origin,
        SeekOrigin.End => _inner.Seek(offset, SeekOrigin.End) - _origin,
        _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, "Unknown seek origin.")
    };

    public override void SetLength(long value) => throw new NotSupportedException("The stream is read-only.");

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException("The stream is read-only.");

    protected override void Dispose(bool disposing)
    {
    }
}
