namespace Ico.Reader.Test.Integration;

/// <summary>
/// Compares decoded pixels against reference data produced by icotool, an independent ICO
/// implementation, so the decoders are checked against something other than themselves.
/// </summary>
public sealed class PixelAccuracyTests
{
    private readonly IcoReader _reader = new();

    [Theory]
    [MemberData(nameof(IcoFixtures.SingleImage), MemberType = typeof(IcoFixtures))]
    public void GetImage_MatchesTheReferenceDecoder(string file, int width, int height, int bitCount)
    {
        var ico = _reader.Read(TestFiles.Ico(file));
        Assert.NotNull(ico);
        Assert.Equal(bitCount, ico.ImageReferences[0].BitCount);

        var decoded = PngImage.Parse(ico.GetImage(0));

        PixelAssert.Matches(TestFiles.ExpectedPixels(file, 0), decoded.Rgba, width, height);
    }

    [Theory]
    [MemberData(nameof(IcoFixtures.MultiImage), MemberType = typeof(IcoFixtures))]
    public void GetImage_MatchesTheReferenceDecoderForEveryEntry(string file, int entries)
    {
        var ico = _reader.Read(TestFiles.Ico(file));
        Assert.NotNull(ico);

        for (var i = 0; i < entries; i++)
        {
            var reference = ico.ImageReferences[i];
            var decoded = PngImage.Parse(ico.GetImage(i));

            PixelAssert.Matches(TestFiles.ExpectedPixels(file, i), decoded.Rgba, reference.Width, reference.Height);
        }
    }

    [Theory]
    [MemberData(nameof(IcoFixtures.Masked), MemberType = typeof(IcoFixtures))]
    public void GetImage_AppliesTheAndMask(string file)
    {
        // The masked fixtures are circular, so the corners must come out fully transparent and the
        // centre fully opaque.
        var ico = _reader.Read(TestFiles.Ico(file));
        Assert.NotNull(ico);

        var image = PngImage.Parse(ico.GetImage(0));

        Assert.Equal(0, AlphaAt(image, 0, 0));
        Assert.Equal(0, AlphaAt(image, image.Width - 1, 0));
        Assert.Equal(0, AlphaAt(image, 0, image.Height - 1));
        Assert.Equal(0, AlphaAt(image, image.Width - 1, image.Height - 1));
        Assert.Equal(255, AlphaAt(image, image.Width / 2, image.Height / 2));
    }

    [Theory]
    [MemberData(nameof(IcoFixtures.UnmaskedWithoutAlphaChannel), MemberType = typeof(IcoFixtures))]
    public void GetImage_LeavesAnEmptyMaskFullyOpaque(string file)
    {
        // Below 32bpp the alpha channel comes purely from the AND mask, which these fixtures leave
        // empty.
        var ico = _reader.Read(TestFiles.Ico(file));
        Assert.NotNull(ico);

        var image = PngImage.Parse(ico.GetImage(0));

        for (var i = 3; i < image.Rgba.Length; i += 4)
            Assert.Equal(255, image.Rgba[i]);
    }

    [Fact]
    public void GetImage_PreservesPartialAlphaFor32Bit()
    {
        var ico = _reader.Read(TestFiles.Ico("icon_256_32bpp.ico"));
        Assert.NotNull(ico);

        var image = PngImage.Parse(ico.GetImage(0));

        var alphas = new HashSet<byte>();
        for (var i = 3; i < image.Rgba.Length; i += 4)
            alphas.Add(image.Rgba[i]);

        Assert.True(alphas.Count > 1, "Expected the 32 bit fixture to carry more than one alpha value.");
    }

    private static byte AlphaAt(PngImage image, int x, int y) => image.Rgba[(((y * image.Width) + x) * 4) + 3];
}
