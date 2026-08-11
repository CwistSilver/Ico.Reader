using Ico.Reader.Decoder;
using Ico.Reader.Export;
using Ico.Reader.Reading;

using Microsoft.Extensions.DependencyInjection;

namespace Ico.Reader.Test.Integration;

/// <summary>
/// Pins the thread-safety contract.
/// <para>
/// <see cref="Data.Source.PathSource"/>, <see cref="Data.Source.MemorySource"/> and
/// <see cref="Data.Source.StreamBufferSource"/> open a fresh stream per call, so an
/// <see cref="IcoData"/> built on any of them serves concurrent readers. A non-copied stream
/// (<c>copyStream: false</c>) is the exception and stays single-reader, which
/// <c>DataSourceTests</c> covers directly rather than by racing.
/// </para>
/// <para>
/// Every case compares against a sequential baseline, so torn or interleaved reads surface as
/// unequal bytes rather than as a crash.
/// </para>
/// </summary>
public sealed class ConcurrencyTests
{
    private const string Fixture = "icon_multi_mixed.ico";
    private const int Iterations = 240;

    private static readonly ParallelOptions _parallel = new() { MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount, 4) };

    private readonly IcoReader _reader = new();

    private IcoData Read(string file) => _reader.Read(TestFiles.Ico(file)) ?? throw new InvalidOperationException($"Could not read {file}.");

    private static byte[][] SequentialBaseline(IcoData ico)
        => [.. Enumerable.Range(0, ico.ImageReferences.Count).Select(ico.GetImage)];

    public static TheoryData<string> SourceKinds => ["path", "bytes", "copied-stream"];

    private IcoData ReadFrom(string sourceKind, string file)
    {
        var path = TestFiles.Ico(file);
        return sourceKind switch
        {
            "path" => _reader.Read(path)!,
            "bytes" => _reader.Read(File.ReadAllBytes(path))!,
            "copied-stream" => ReadCopiedStream(path),
            _ => throw new ArgumentOutOfRangeException(nameof(sourceKind), sourceKind, "Unknown source kind.")
        };

        IcoData ReadCopiedStream(string p)
        {
            using var stream = File.OpenRead(p);
            return _reader.Read(stream, copyStream: true)!;
        }
    }

    [Theory]
    [MemberData(nameof(SourceKinds))]
    public void GetImage_ReturnsTheSameBytesUnderParallelUse(string sourceKind)
    {
        var ico = ReadFrom(sourceKind, Fixture);
        var expected = SequentialBaseline(ico);
        var results = new byte[Iterations][];

        Parallel.For(0, Iterations, _parallel, i => results[i] = ico.GetImage(i % ico.ImageReferences.Count));

        for (var i = 0; i < Iterations; i++)
            Assert.Equal(expected[i % expected.Length], results[i]);
    }

    [Fact]
    public async Task GetImageAsync_ReturnsTheSameBytesUnderParallelUse()
    {
        var ico = Read(Fixture);
        var expected = SequentialBaseline(ico);

        var results = await Task.WhenAll(Enumerable.Range(0, Iterations)
            .Select(i => Task.Run(() => ico.GetImageAsync(i % ico.ImageReferences.Count, TestContext.Current.CancellationToken))));

        for (var i = 0; i < Iterations; i++)
            Assert.Equal(expected[i % expected.Length], results[i]);
    }

    [Fact]
    public async Task GetImage_AndGetImageAsync_CanRunAgainstTheSameIcoDataAtOnce()
    {
        var ico = Read(Fixture);
        var expected = SequentialBaseline(ico);

        var tasks = Enumerable.Range(0, Iterations).Select(i =>
        {
            var index = i % ico.ImageReferences.Count;
            return i % 2 == 0
                ? Task.Run(() => ico.GetImage(index))
                : Task.Run(() => ico.GetImageAsync(index, TestContext.Current.CancellationToken));
        });

        var results = await Task.WhenAll(tasks);

        for (var i = 0; i < Iterations; i++)
            Assert.Equal(expected[i % expected.Length], results[i]);
    }

    [Fact]
    public void GetImage_IsSafeAcrossTheWholeFixtureSet()
    {
        // Different bit depths take different decoder paths, so this spreads the load over all of
        // them at once rather than hammering a single decoder.
        var files = new[] { "icon_32_1bpp.ico", "icon_32_4bpp.ico", "icon_32_8bpp.ico", "icon_32_24bpp.ico", "icon_32_32bpp.ico", IcoFixtures.PngEmbedded };
        var icos = files.Select(Read).ToArray();
        var expected = icos.Select(SequentialBaseline).ToArray();
        var results = new byte[Iterations][];

        Parallel.For(0, Iterations, _parallel, i => results[i] = icos[i % icos.Length].GetImage(0));

        for (var i = 0; i < Iterations; i++)
            Assert.Equal(expected[i % icos.Length][0], results[i]);
    }

    [Theory]
    [InlineData("icon_32_1bpp.ico", "icon_32_1bpp_masked.ico")]
    [InlineData("icon_32_4bpp.ico", "icon_32_4bpp_masked.ico")]
    [InlineData("icon_32_8bpp.ico", "icon_32_8bpp_masked.ico")]
    [InlineData("icon_32_24bpp.ico", "icon_32_24bpp_masked.ico")]
    [InlineData("icon_32_32bpp.ico", "icon_32_32bpp_masked.ico")]
    public void GetImage_IsSafeWhenOneDecoderServesDifferentImagesOfTheSameShape(string first, string second)
    {
        // One IcoReader shares a single decoder instance across every IcoData it produces, and
        // these two files have identical dimensions and bit depth but different pixels. A decoder
        // that cached a scratch buffer would clear any size check and then return the other
        // image's content, which decoding the same image repeatedly could never reveal.
        var a = Read(first);
        var b = Read(second);
        var expectedA = a.GetImage(0);
        var expectedB = b.GetImage(0);
        Assert.NotEqual(expectedA, expectedB);

        var results = new (byte[] Actual, byte[] Expected)[Iterations];

        Parallel.For(0, Iterations, _parallel, i => results[i] = i % 2 == 0
            ? (a.GetImage(0), expectedA)
            : (b.GetImage(0), expectedB));

        foreach (var (actual, expected) in results)
            Assert.Equal(expected, actual);
    }

    [Fact]
    public void Read_IsSafeWhenOneReaderIsSharedAcrossThreads()
    {
        // IcoReader holds only its configuration, and the decoders inside it are stateless, so a
        // single instance is expected to serve every caller.
        var files = new[] { "icon_16_4bpp.ico", "icon_32_8bpp.ico", "icon_48_32bpp.ico", "icon_multi.ico" };
        var expected = files.Select(f => SequentialBaseline(Read(f))).ToArray();
        var results = new byte[Iterations][][];

        Parallel.For(0, Iterations, _parallel, i =>
        {
            var ico = _reader.Read(TestFiles.Ico(files[i % files.Length]));
            results[i] = SequentialBaseline(ico!);
        });

        for (var i = 0; i < Iterations; i++)
            Assert.Equal(expected[i % files.Length], results[i]);
    }

    [Fact]
    public void Read_IsSafeWhenTheSameFileIsParsedInParallel()
    {
        var expected = SequentialBaseline(Read(Fixture));
        var results = new byte[Iterations][][];

        Parallel.For(0, Iterations, _parallel, i => results[i] = SequentialBaseline(_reader.Read(TestFiles.Ico(Fixture))!));

        Assert.All(results, actual => Assert.Equal(expected, actual));
    }

    [Fact]
    public void SharedSingletonDecoder_IsSafeAcrossThreads()
    {
        // AddIcoReader registers IIcoDecoder as a singleton, so in a DI application one instance
        // serves every request.
        using var provider = new ServiceCollection().AddIcoReader().BuildServiceProvider();
        var reader = provider.GetRequiredService<IcoReader>();
        var decoder = provider.GetRequiredService<IIcoDecoder>();

        var ico = reader.Read(TestFiles.Ico(Fixture))!;
        var expected = SequentialBaseline(ico);
        var references = ico.ImageReferences;
        var results = new byte[Iterations][];

        Parallel.For(0, Iterations, _parallel, i =>
        {
            var index = i % references.Count;
            using var stream = File.OpenRead(TestFiles.Ico(Fixture));
            results[i] = ImageDataReader.Read(stream, references[index], decoder);
        });

        for (var i = 0; i < Iterations; i++)
            Assert.Equal(expected[i % expected.Length], results[i]);
    }

    [Fact]
    public void PeSource_GetImage_IsSafeUnderParallelUse()
    {
        // The PE path resolves images through group directory entries, so it touches more shared
        // state than a standalone ICO does.
        var pe = _reader.Read(TestFiles.PeFixture)!;
        var expected = SequentialBaseline(pe);
        var results = new byte[Iterations][];

        Parallel.For(0, Iterations, _parallel, i => results[i] = pe.GetImage(i % pe.ImageReferences.Count));

        for (var i = 0; i < Iterations; i++)
            Assert.Equal(expected[i % expected.Length], results[i]);
    }

    [Fact]
    public void ReadOnlyQueries_AreSafeUnderParallelUse()
    {
        var pe = _reader.Read(TestFiles.PeFixture)!;
        var expectedPreferred = pe.PreferredImageIndex();
        var expectedGroupSizes = pe.Groups.Select(x => x.Size).ToArray();

        Parallel.For(0, Iterations, _parallel, _ =>
        {
            Assert.Equal(expectedPreferred, pe.PreferredImageIndex());
            Assert.Equal(expectedGroupSizes, pe.Groups.Select(x => x.Size));
            Assert.Equal(IcoType.Icon, pe.GetIconGroup("2").IcoType);
            Assert.Equal(IcoType.Cursor, pe.GetCursorGroup("2").IcoType);
        });
    }

    [Fact]
    public async Task SaveAllImagesToDirectory_WritesCorrectContentDespiteItsOwnConcurrency()
    {
        // The method fans every image out over Task.WhenAll internally, so this exercises the
        // library's own parallelism rather than the caller's.
        var pe = _reader.Read(TestFiles.PeFixture)!;
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ico-reader-concurrency-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            await new IcoExporter().SaveAllImagesToDirectoryAsync(pe, outputDirectory, TestContext.Current.CancellationToken);

            var root = Path.Combine(outputDirectory, "Ico.Reader.Test.PeFixture");
            var written = Directory.GetFiles(root);
            Assert.Equal(pe.ImageReferences.Count, written.Length);

            var expected = SequentialBaseline(pe).Select(x => x.Length).OrderBy(x => x);
            Assert.Equal(expected, written.Select(x => (int)new FileInfo(x).Length).OrderBy(x => x));

            foreach (var file in written)
                _ = PngImage.Parse(await File.ReadAllBytesAsync(file, TestContext.Current.CancellationToken));
        }
        finally
        {
            Directory.Delete(outputDirectory, recursive: true);
        }
    }
}
