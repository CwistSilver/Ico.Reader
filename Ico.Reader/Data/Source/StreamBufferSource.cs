using Ico.Reader.Data.IcoSources;

namespace Ico.Reader.Data.Source;
internal sealed class StreamBufferSource : IIcoSource
{
    private readonly byte[] _buffer;

    internal StreamBufferSource(Stream sourceStream)
    {
        if (sourceStream is null) throw new ArgumentNullException(nameof(sourceStream));

        var startPosition = sourceStream.Position;

        using var ms = new MemoryStream();
        sourceStream.CopyTo(ms);
        _buffer = ms.ToArray();

        sourceStream.Position = startPosition;
    }

    public Stream GetStream(bool useAsync = false) => new MemoryStream(_buffer, writable: false);
}

