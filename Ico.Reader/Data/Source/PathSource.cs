namespace Ico.Reader.Data.Source;

/// <summary>
/// An <see cref="IDataSource"/> that reads from a file on demand, so image data never has to be
/// held in memory.
/// </summary>
public sealed class PathSource : IDataSource
{
    private readonly string _originPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="PathSource"/> class.
    /// </summary>
    /// <param name="path">The path of the file containing the ico data.</param>
    /// <exception cref="ArgumentException">The path is null, empty or whitespace.</exception>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    public PathSource(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("The path cannot be null or empty.", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException("The file does not exist.", path);

        _originPath = path;
    }

    /// <inheritdoc/>
    public Stream GetStream(bool useAsync = false) => new FileStream(_originPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, useAsync);
}
