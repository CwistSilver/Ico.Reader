namespace Ico.Reader.Test.Integration;

/// <summary>
/// The reader accepts a path, a byte array or a stream. All three have to yield the same images.
/// </summary>
public sealed class ReadSourceTests
{
    private const string Fixture = "icon_multi_mixed.ico";

    private readonly IcoReader _reader = new();

    [Fact]
    public void Read_ProducesTheSameImagesFromEverySource()
    {
        var path = TestFiles.Ico(Fixture);
        var bytes = File.ReadAllBytes(path);

        var fromPath = _reader.Read(path);
        var fromBytes = _reader.Read(bytes);
        using var copiedStream = new MemoryStream(bytes);
        var fromCopiedStream = _reader.Read(copiedStream);
        using var directStream = new MemoryStream(bytes);
        var fromDirectStream = _reader.Read(directStream, copyStream: false);

        Assert.NotNull(fromPath);
        Assert.NotNull(fromBytes);
        Assert.NotNull(fromCopiedStream);
        Assert.NotNull(fromDirectStream);

        for (var i = 0; i < fromPath.ImageReferences.Count; i++)
        {
            var expected = fromPath.GetImage(i);
            Assert.Equal(expected, fromBytes.GetImage(i));
            Assert.Equal(expected, fromCopiedStream.GetImage(i));
            Assert.Equal(expected, fromDirectStream.GetImage(i));
        }
    }

    [Fact]
    public void Read_OnlyNamesTheResultWhenReadingFromAPath()
    {
        var bytes = TestFiles.IcoBytes(Fixture);

        Assert.Equal(string.Empty, _reader.Read(bytes)!.Name);
        Assert.Equal(Path.GetFileNameWithoutExtension(Fixture), _reader.Read(TestFiles.Ico(Fixture))!.Name);
    }

    [Fact]
    public void Read_WithACopiedStream_KeepsWorkingAfterTheOriginIsClosed()
    {
        IcoData? ico;
        using (var stream = File.OpenRead(TestFiles.Ico(Fixture)))
            ico = _reader.Read(stream, copyStream: true);

        Assert.NotNull(ico);
        Assert.NotEmpty(ico.GetImage(0));
    }

    [Fact]
    public async Task GetImageAsync_MatchesGetImage()
    {
        var ico = _reader.Read(TestFiles.Ico(Fixture));
        Assert.NotNull(ico);

        foreach (var reference in ico.ImageReferences)
            Assert.Equal(ico.GetImage(reference), await ico.GetImageAsync(reference));
    }

    [Fact]
    public async Task GetImageAsync_MatchesGetImageForPngEntries()
    {
        var ico = _reader.Read(TestFiles.Ico(IcoFixtures.PngEmbedded));
        Assert.NotNull(ico);

        Assert.Equal(ico.GetImage(0), await ico.GetImageAsync(0));
    }

    [Fact]
    public void GetImage_CanBeCalledRepeatedly()
    {
        // Each call opens a fresh stream from the data source and disposes it afterwards, so a
        // source that handed out a single shared stream would fail on the second call.
        var ico = _reader.Read(TestFiles.Ico(Fixture));
        Assert.NotNull(ico);

        var first = ico.GetImage(0);
        var second = ico.GetImage(0);
        var third = ico.GetImage(ico.ImageReferences[0]);

        Assert.NotEmpty(first);
        Assert.Equal(first, second);
        Assert.Equal(first, third);
    }

    [Fact]
    public void GetImage_CanBeCalledRepeatedlyOnADirectStream()
    {
        using var stream = File.OpenRead(TestFiles.Ico(Fixture));
        var ico = _reader.Read(stream, copyStream: false);
        Assert.NotNull(ico);

        Assert.Equal(ico.GetImage(0), ico.GetImage(0));
    }

    [Fact]
    public void Read_WithADirectStream_FailsOnceTheOriginIsClosed()
    {
        // The README warns that a non-copied stream must stay open until every image is read.
        IcoData? ico;
        using (var stream = File.OpenRead(TestFiles.Ico(Fixture)))
            ico = _reader.Read(stream, copyStream: false);

        Assert.NotNull(ico);

        var exception = Assert.Throws<ObjectDisposedException>(() => ico.GetImage(0));
        Assert.Contains("copyStream", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReadAsync_WithADirectStream_FailsOnceTheOriginIsClosed()
    {
        IcoData? ico;
        using (var stream = File.OpenRead(TestFiles.Ico(Fixture)))
            ico = _reader.Read(stream, copyStream: false);

        Assert.NotNull(ico);

        await Assert.ThrowsAsync<ObjectDisposedException>(() => ico.GetImageAsync(0));
    }
}
