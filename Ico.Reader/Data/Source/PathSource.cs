namespace Ico.Reader.Data.Source;

public sealed class PathSource : IDataSource
{
    private readonly string _originPath;
    public PathSource(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("The path cannot be null or empty.", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException("The file does not exist.", path);

        _originPath = path;
    }

    public Stream GetStream(bool useAsync = false) => new FileStream(_originPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, useAsync);
}
