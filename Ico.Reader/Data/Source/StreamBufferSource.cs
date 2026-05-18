namespace Ico.Reader.Data.Source;

public sealed class StreamBufferSource : IDataSource
{
    private readonly byte[] _buffer;

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

    public Stream GetStream(bool useAsync = false) => new MemoryStream(_buffer, writable: false);
}

