namespace Ico.Reader.Data.Source;

/// <summary>
/// An <see cref="IDataSource"/> that copies a stream up front, so images stay readable after the
/// caller closes the original.
/// </summary>
public sealed class StreamBufferSource : IDataSource
{
    private readonly byte[] _buffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamBufferSource"/> class, copying the stream
    /// into memory and restoring its original position.
    /// </summary>
    /// <param name="sourceStream">The stream to copy. It is left open and repositioned where it started.</param>
    /// <exception cref="ArgumentNullException">The stream is null.</exception>
    /// <exception cref="ArgumentException">The stream is not readable.</exception>
    public StreamBufferSource(Stream sourceStream)
    {
        if (sourceStream is null)
            throw new ArgumentNullException(nameof(sourceStream));

        if (!sourceStream.CanRead)
            throw new ArgumentException("The source stream must be readable.", nameof(sourceStream));

        var startPosition = sourceStream.Position;

        using var ms = new MemoryStream();
        sourceStream.CopyTo(ms);
        _buffer = ms.ToArray();

        sourceStream.Position = startPosition;
    }

    /// <inheritdoc/>
    public Stream GetStream(bool useAsync = false) => new MemoryStream(_buffer, writable: false);
}

