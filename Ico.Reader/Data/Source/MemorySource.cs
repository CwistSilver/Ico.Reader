namespace Ico.Reader.Data.Source;

/// <summary>
/// An <see cref="IDataSource"/> over ico data already held in a byte array.
/// </summary>
public sealed class MemorySource : IDataSource
{
    private readonly byte[] _data;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemorySource"/> class.
    /// </summary>
    /// <param name="data">The ico data. It is used directly rather than copied.</param>
    public MemorySource(byte[] data)
    {
        _data = data;
    }

    /// <inheritdoc/>
    public Stream GetStream(bool useAsync = false) => new MemoryStream(_data, false);
}
