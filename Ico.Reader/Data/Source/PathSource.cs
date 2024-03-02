using Ico.Reader.Data.IcoSources;

namespace Ico.Reader.Data.Source;
internal sealed class PathSource : IIcoSource
{
    private readonly string _originPath;
    internal PathSource(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("The path cannot be null or empty.", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException("The file does not exist.", path);

        _originPath = path;
    }

    public Stream GetStream(bool useAsync = false) => new FileStream(_originPath, FileMode.Open, FileAccess.Read, FileShare.Read, 0, useAsync);
}
