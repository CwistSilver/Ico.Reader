namespace Ico.Reader.Data.Source;

public sealed class StreamSource : IDataSource
{
    private readonly Stream _sourceStream;
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

    public Stream GetStream(bool useAsync = false)
    {
        if (!_sourceStream.CanRead)
            throw new ArgumentException("The source stream must be readable.", nameof(_sourceStream));

        if (!_sourceStream.CanSeek)
            throw new ArgumentException("The source stream must be seekable.", nameof(_sourceStream));

        return _sourceStream;
    }
}

