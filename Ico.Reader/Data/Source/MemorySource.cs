namespace Ico.Reader.Data.Source;

public sealed class MemorySource : IDataSource
{
    private readonly byte[] _data;

    public MemorySource(byte[] data)
    {
        _data = data;
    }

    public Stream GetStream(bool useAsync = false) => new MemoryStream(_data, false);
}
