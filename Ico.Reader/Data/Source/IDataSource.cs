namespace Ico.Reader.Data.Source;

/// <summary>
/// Supplies the ico data on demand, so that images can be read lazily rather than all being held
/// in memory.
/// </summary>
public interface IDataSource
{
    /// <summary>
    /// Opens a stream over the ico data.
    /// </summary>
    /// <param name="useAsync">Whether the stream will be read asynchronously. Sources backed by a
    /// file open in asynchronous mode; in-memory sources ignore it.</param>
    /// <returns>A readable, seekable stream positioned at the start of the data.</returns>
    Stream GetStream(bool useAsync = false);
}
