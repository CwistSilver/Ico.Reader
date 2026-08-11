using Ico.Reader.Export;

namespace Ico.Reader.Test.Integration;

public sealed class AsyncApiTests : IDisposable
{
    private const string Fixture = "icon_multi_mixed.ico";

    private readonly IcoReader _reader = new();
    private readonly IIcoExporter _exporter = new IcoExporter();
    private readonly string _outputDirectory = Path.Combine(Path.GetTempPath(), $"ico-reader-async-{Guid.NewGuid():N}");

    public AsyncApiTests() => Directory.CreateDirectory(_outputDirectory);

    public void Dispose()
    {
        if (Directory.Exists(_outputDirectory))
            Directory.Delete(_outputDirectory, recursive: true);
    }

    [Fact]
    public async Task ReadAsync_FromPathMatchesTheSynchronousRead()
    {
        var path = TestFiles.Ico(Fixture);
        var expected = _reader.Read(path);
        var actual = await _reader.ReadAsync(path, TestContext.Current.CancellationToken);

        Assert.NotNull(expected);
        Assert.NotNull(actual);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.OriginFileType, actual.OriginFileType);
        Assert.Equal(expected.ImageReferences.Count, actual.ImageReferences.Count);

        for (var i = 0; i < expected.ImageReferences.Count; i++)
            Assert.Equal(expected.GetImage(i), actual.GetImage(i));
    }

    [Fact]
    public async Task ReadAsync_FromStreamMatchesTheSynchronousRead()
    {
        var path = TestFiles.Ico(Fixture);
        var expected = _reader.Read(path);
        Assert.NotNull(expected);

        using var stream = File.OpenRead(path);
        var actual = await _reader.ReadAsync(stream, TestContext.Current.CancellationToken);

        Assert.NotNull(actual);
        Assert.Equal(expected.ImageReferences.Count, actual.ImageReferences.Count);
        Assert.Equal(expected.GetImage(0), actual.GetImage(0));
    }

    [Fact]
    public async Task ReadAsync_ReadsAPeFile()
    {
        var pe = await _reader.ReadAsync(TestFiles.PeFixture, TestContext.Current.CancellationToken);

        Assert.NotNull(pe);
        Assert.Equal(IcoOriginFileType.Dll, pe.OriginFileType);
        Assert.Equal(13, pe.ImageReferences.Count);
    }

    [Fact]
    public async Task ReadAsync_ReturnsNullForAMissingFile()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.ico");

        Assert.Null(await _reader.ReadAsync(missing, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReadAsync_LeavesTheCallerStreamOpen()
    {
        using var stream = File.OpenRead(TestFiles.Ico(Fixture));

        _ = await _reader.ReadAsync(stream, TestContext.Current.CancellationToken);

        Assert.True(stream.CanRead);
    }

    [Fact]
    public async Task ReadAsync_RejectsANullStream()
        => await Assert.ThrowsAsync<ArgumentNullException>(() => _reader.ReadAsync((Stream)null!, TestContext.Current.CancellationToken));

    [Fact]
    public async Task ReadAsync_HonoursACancelledToken()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _reader.ReadAsync(TestFiles.Ico(Fixture), cancelled.Token));
    }

    [Fact]
    public async Task GetImageAsync_HonoursACancelledToken()
    {
        var ico = _reader.Read(TestFiles.Ico(Fixture));
        Assert.NotNull(ico);

        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ico.GetImageAsync(0, cancelled.Token));
    }

    [Fact]
    public async Task SaveImageAsync_HonoursACancelledToken()
    {
        var ico = _reader.Read(TestFiles.Ico(Fixture));
        Assert.NotNull(ico);

        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        var path = Path.Combine(_outputDirectory, "cancelled.png");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _exporter.SaveImageAsync(ico, ico.ImageReferences[0], path, cancelled.Token));
    }

    [Fact]
    public async Task SaveAllImagesToDirectoryAsync_HonoursACancelledToken()
    {
        var ico = _reader.Read(TestFiles.Ico(Fixture));
        Assert.NotNull(ico);

        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _exporter.SaveAllImagesToDirectoryAsync(ico, _outputDirectory, cancelled.Token));
    }

    [Fact]
    public async Task AsyncApi_CompletesNormallyWithAnUncancelledToken()
    {
        // Positive control, so the cancellation cases above cannot pass for the wrong reason.
        using var live = new CancellationTokenSource();

        var ico = await _reader.ReadAsync(TestFiles.Ico(Fixture), live.Token);
        Assert.NotNull(ico);

        Assert.NotEmpty(await ico.GetImageAsync(0, live.Token));
        await _exporter.SaveAllImagesToDirectoryAsync(ico, _outputDirectory, live.Token);

        Assert.Equal(ico.ImageReferences.Count, Directory.GetFiles(_outputDirectory, "*.png", SearchOption.AllDirectories).Length);
    }
}
