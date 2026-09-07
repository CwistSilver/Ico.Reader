namespace Ico.Reader.Data.Source;

/// <summary>
/// An <see cref="IDataSource"/> that reads the caller's stream directly, without copying it. The
/// stream must stay open for as long as images are read from it.
/// </summary>
public sealed class StreamSource : IDataSource
{
    private readonly Stream _sourceStream;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamSource"/> class.
    /// </summary>
    /// <param name="sourceStream">The stream to read from. Ownership stays with the caller.</param>
    /// <exception cref="ArgumentNullException">The stream is null.</exception>
    /// <exception cref="ArgumentException">The stream is not readable or not seekable.</exception>
    public StreamSource(Stream sourceStream)
    {
        if (sourceStream is null)
            throw new ArgumentNullException(nameof(sourceStream));

        if (!sourceStream.CanRead)
            throw new ArgumentException("The source stream must be readable.", nameof(sourceStream));

        if (!sourceStream.CanSeek)
            throw new ArgumentException("The source stream must be seekable.", nameof(sourceStream));

        _sourceStream = sourceStream;
    }

    /// <summary>
    /// Returns the caller's stream behind a wrapper, so that consumers disposing what they get back
    /// do not close a stream this source does not own.
    /// </summary>
    /// <returns>A stream over the ico data.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the caller has closed the source stream.</exception>
    public Stream GetStream(bool useAsync = false)
    {
        // The constructor required a readable, seekable stream, so losing either capability means
        // the caller closed it.
        if (!_sourceStream.CanRead || !_sourceStream.CanSeek)
        {
            throw new ObjectDisposedException(
                _sourceStream.GetType().Name,
                "The source stream has been closed. A stream read with copyStream set to false must stay open until every image has been read.");
        }

        return new NonDisposingStream(_sourceStream);
    }
}

