namespace Ico.Reader.Test.Infrastructure;

/// <summary>
/// Resolves the paths of the committed test fixtures. Regenerate them with
/// <c>Tools/generate-fixtures.ps1</c>.
/// </summary>
internal static class TestFiles
{
    private static readonly string _resourcesRoot = Path.Combine(AppContext.BaseDirectory, "Resources");

    public static string SamplePng => Path.Combine(_resourcesRoot, "sample.png");

    public static string PeFixture => Path.Combine(_resourcesRoot, "Pe", "Ico.Reader.Test.PeFixture.dll");

    public static string Ico(string name) => Require(Path.Combine(_resourcesRoot, "Ico", name));

    public static byte[] IcoBytes(string name) => File.ReadAllBytes(Ico(name));

    public static string Cur(string name) => Require(Path.Combine(_resourcesRoot, "Cur", name));

    public static byte[] CurBytes(string name) => File.ReadAllBytes(Cur(name));

    /// <summary>
    /// Loads the reference pixels for one entry of an ICO fixture, decoded by icotool at fixture
    /// generation time and stored as gzipped raw RGBA.
    /// </summary>
    public static byte[] ExpectedPixels(string icoName, int entryIndex)
    {
        var name = $"{Path.GetFileNameWithoutExtension(icoName)}.{entryIndex}.rgba.gz";
        var path = Require(Path.Combine(_resourcesRoot, "Expected", name));

        using var file = File.OpenRead(path);
        using var gzip = new System.IO.Compression.GZipStream(file, System.IO.Compression.CompressionMode.Decompress);
        using var buffer = new MemoryStream();
        gzip.CopyTo(buffer);

        return buffer.ToArray();
    }

    private static string Require(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Missing test fixture. Run Tools/generate-fixtures.ps1 to create it.", path);

        return path;
    }
}
