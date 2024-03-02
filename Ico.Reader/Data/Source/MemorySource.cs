using Ico.Reader.Data.IcoSources;

namespace Ico.Reader.Data.Source;
internal sealed class MemorySource : IIcoSource
{
    private readonly byte[] _data;
    internal MemorySource(byte[] data) => _data = data;
    public Stream GetStream(bool useAsync = false) => new MemoryStream(_data, false);
}
