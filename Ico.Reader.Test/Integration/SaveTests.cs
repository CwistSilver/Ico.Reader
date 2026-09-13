using Ico.Reader.Export;

namespace Ico.Reader.Test.Integration;

public sealed class SaveTests : IDisposable
{
    private readonly IcoReader _reader = new();
    private readonly IIcoExporter _exporter = new IcoExporter();
    private readonly string _outputDirectory = Path.Combine(Path.GetTempPath(), $"ico-reader-tests-{Guid.NewGuid():N}");

    public SaveTests() => Directory.CreateDirectory(_outputDirectory);

    public void Dispose()
    {
        if (Directory.Exists(_outputDirectory))
            Directory.Delete(_outputDirectory, recursive: true);
    }

    private IcoData Read(string file = "icon_multi.ico")
    {
        var ico = _reader.Read(TestFiles.Ico(file));
        Assert.NotNull(ico);
        return ico;
    }

    [Fact]
    public async Task SaveImageAsync_WritesTheDecodedPng()
    {
        var ico = Read();
        var path = Path.Combine(_outputDirectory, "image.png");

        await _exporter.SaveImageAsync(ico, ico.ImageReferences[0], path, TestContext.Current.CancellationToken);

        Assert.Equal(ico.GetImage(0), await AsyncFile.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveImageAsync_OverwritesAnExistingFile()
    {
        // The stale file is deliberately longer than the image, so a truncating write that left
        // trailing bytes behind would show up.
        var ico = Read();
        var path = Path.Combine(_outputDirectory, "image.png");
        await AsyncFile.WriteAllBytesAsync(path, [.. Enumerable.Repeat((byte)0xAB, 10_000)], TestContext.Current.CancellationToken);

        await _exporter.SaveImageAsync(ico, ico.ImageReferences[0], path, TestContext.Current.CancellationToken);

        Assert.Equal(ico.GetImage(0), await AsyncFile.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveImageAsync_ByGroupWritesTheSameBytes()
    {
        var ico = Read();
        var path = Path.Combine(_outputDirectory, "group-image.png");

        await _exporter.SaveImageAsync(ico, ico.GetImageReference(ico.Groups[0], 1), path, TestContext.Current.CancellationToken);

        Assert.Equal(ico.GetImage(1), await AsyncFile.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveGroupToDirectory_WritesOneFilePerEntry()
    {
        var ico = Read();

        await _exporter.SaveGroupToDirectoryAsync(ico, ico.Groups[0], _outputDirectory, TestContext.Current.CancellationToken);

        var groupDirectory = Path.Combine(_outputDirectory, "Icon", "Group 1");
        var files = Directory.GetFiles(groupDirectory);
        Assert.Equal(3, files.Length);
        Assert.All(files, file => Assert.EndsWith(".png", file, StringComparison.Ordinal));
    }

    [Fact]
    public async Task SaveAllGroupsToDirectory_NestsUnderTheIcoName()
    {
        var ico = Read();

        await _exporter.SaveAllGroupsToDirectoryAsync(ico, _outputDirectory, TestContext.Current.CancellationToken);

        var groupDirectory = Path.Combine(_outputDirectory, "icon_multi", "Icon", "Group 1");
        Assert.True(Directory.Exists(groupDirectory));
        Assert.Equal(3, Directory.GetFiles(groupDirectory).Length);
    }

    [Fact]
    public async Task SaveAllImagesToDirectory_NamesFilesAfterTheImageMetadata()
    {
        var ico = Read();

        await _exporter.SaveAllImagesToDirectoryAsync(ico, _outputDirectory, TestContext.Current.CancellationToken);

        var files = Directory.GetFiles(Path.Combine(_outputDirectory, "icon_multi")).Select(Path.GetFileName).ToArray();
        Assert.Equal(3, files.Length);
        Assert.Contains("0_icon_multi_Icon (16x16 32 bit).png", files);
        Assert.Contains("1_icon_multi_Icon (32x32 32 bit).png", files);
        Assert.Contains("2_icon_multi_Icon (48x48 32 bit).png", files);
    }

    [Fact]
    public async Task SaveAllImagesToDirectory_KeepsAnIconAndACursorSharingAnIdApart()
    {
        // Resource ids are only unique within their type, so an icon and a cursor of the same size can
        // share the id the file name starts with.
        var pe = await AsyncFile.ReadAllBytesAsync(TestFiles.PeFixture, TestContext.Current.CancellationToken);
        PeResources.NumberCursorsFromOne(pe);
        var path = Path.Combine(_outputDirectory, "shared-ids.dll");
        await AsyncFile.WriteAllBytesAsync(path, pe, TestContext.Current.CancellationToken);
        var ico = _reader.Read(path);
        Assert.NotNull(ico);
        var exportDirectory = Path.Combine(_outputDirectory, "export");

        await _exporter.SaveAllImagesToDirectoryAsync(ico, exportDirectory, TestContext.Current.CancellationToken);

        Assert.Equal(ico.ImageReferences.Count, Directory.GetFiles(Path.Combine(exportDirectory, "shared-ids")).Length);
    }

    [Fact]
    public async Task SaveAllImagesToDirectory_FallsBackToTheImageTypeWhenUnnamed()
    {
        var ico = _reader.Read(TestFiles.IcoBytes("icon_32_8bpp.ico"));
        Assert.NotNull(ico);

        await _exporter.SaveAllImagesToDirectoryAsync(ico, _outputDirectory, TestContext.Current.CancellationToken);

        var root = Directory.GetDirectories(_outputDirectory).Single();
        var file = Path.GetFileName(Directory.GetFiles(root).Single());
        Assert.Equal("0_Icon (32x32 8 bit).png", file);
    }

    [Fact]
    public async Task SaveAllGroupsToDirectory_SeparatesIconsFromCursors()
    {
        // A PE carries several icon and cursor groups at once, and icon group "1" and cursor
        // group "1" only differ by type. A single-group ICO can never reach this branch.
        var pe = _reader.Read(TestFiles.PeFixture);
        Assert.NotNull(pe);

        await _exporter.SaveAllGroupsToDirectoryAsync(pe, _outputDirectory, TestContext.Current.CancellationToken);

        var root = Path.Combine(_outputDirectory, "Ico.Reader.Test.PeFixture");
        Assert.Equal(["Cursor", "Icon"], Directory.GetDirectories(root).Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal));

        foreach (var group in pe.Groups)
        {
            var groupPath = Path.Combine(root, group.IcoType.ToString(), $"Group {group.Name}");
            Assert.True(Directory.Exists(groupPath), $"Missing directory for {group.IcoType} group {group.Name}.");
            Assert.Equal(group.Size, Directory.GetFiles(groupPath).Length);
        }
    }

    [Fact]
    public async Task SaveAllGroupsToDirectory_WritesEveryImageOfAMultiGroupSource()
    {
        var pe = _reader.Read(TestFiles.PeFixture);
        Assert.NotNull(pe);

        await _exporter.SaveAllGroupsToDirectoryAsync(pe, _outputDirectory, TestContext.Current.CancellationToken);

        var root = Path.Combine(_outputDirectory, "Ico.Reader.Test.PeFixture");
        var written = Directory.GetFiles(root, "*.png", SearchOption.AllDirectories);

        Assert.Equal(pe.Groups.Sum(x => x.Size), written.Length);
        foreach (var file in written)
            _ = PngImage.Parse(await AsyncFile.ReadAllBytesAsync(file, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SavedImagesAreValidPngFiles()
    {
        var ico = Read("icon_multi_png_bmp.ico");

        await _exporter.SaveAllImagesToDirectoryAsync(ico, _outputDirectory, TestContext.Current.CancellationToken);

        foreach (var file in Directory.GetFiles(Path.Combine(_outputDirectory, "icon_multi_png_bmp")))
            _ = PngImage.Parse(await AsyncFile.ReadAllBytesAsync(file, TestContext.Current.CancellationToken));
    }
}
