using Ico.Reader.Decoder.ImageDecoder.Bmp;

namespace Ico.Reader.Test.Unit.Decoder;

/// <summary>
/// Pixel-level tests for the per-bit-depth BMP decoders. ICO stores rows bottom-up, colours as BGR
/// and transparency in a separate 1-bit AND mask, so those three conversions are what these cover.
/// </summary>
public sealed class IcoBmpDecoderTests
{
    private static readonly Rgb _red = new(255, 0, 0);
    private static readonly Rgb _green = new(0, 255, 0);
    private static readonly Rgb _blue = new(0, 0, 255);
    private static readonly Rgb _black = new(0, 0, 0);
    private static readonly Rgb _white = new(255, 255, 255);

    private static BmpInfoHeader Header(int width, int height, ushort bitCount, int paletteLength) => new()
    {
        Size = 40,
        Width = width,
        Height = height * 2,
        Planes = 1,
        BitCount = bitCount,
        ClrUsed = paletteLength
    };

    private static Rgb PixelAt(byte[] rgba, int width, int x, int y)
    {
        var i = ((y * width) + x) * 4;
        return new Rgb(rgba[i], rgba[i + 1], rgba[i + 2]);
    }

    private static byte AlphaAt(byte[] rgba, int width, int x, int y) => rgba[(((y * width) + x) * 4) + 3];

    [Fact]
    public void Bmp1_MapsPaletteIndicesAndKeepsRowOrder()
    {
        Rgb[] palette = [_black, _white];
        var indices = new byte[,] { { 0, 1 }, { 1, 0 } };
        var image = IcoBmpImage.Indexed(1, palette, indices);

        var rgba = new IcoBmp1Decoder().DecodeIcoBmpToRgba(image, Header(2, 2, 1, 2));

        Assert.Equal(_black, PixelAt(rgba, 2, 0, 0));
        Assert.Equal(_white, PixelAt(rgba, 2, 1, 0));
        Assert.Equal(_white, PixelAt(rgba, 2, 0, 1));
        Assert.Equal(_black, PixelAt(rgba, 2, 1, 1));
    }

    [Fact]
    public void Bmp1_HonoursTheAndMask()
    {
        Rgb[] palette = [_black, _white];
        var indices = new byte[,] { { 1, 1 }, { 1, 1 } };
        var transparent = new[,] { { true, false }, { false, false } };
        var image = IcoBmpImage.Indexed(1, palette, indices, transparent);

        var rgba = new IcoBmp1Decoder().DecodeIcoBmpToRgba(image, Header(2, 2, 1, 2));

        Assert.Equal(0, AlphaAt(rgba, 2, 0, 0));
        Assert.Equal(255, AlphaAt(rgba, 2, 1, 0));
        Assert.Equal(255, AlphaAt(rgba, 2, 0, 1));
    }

    [Fact]
    public void Bmp1_SwapsThePaletteWhenEveryPixelIsMasked()
    {
        // A fully masked image would render as nothing, so the decoder inverts the palette to make
        // it visible. This pins that fallback.
        Rgb[] palette = [_black, _white];
        var indices = new byte[,] { { 0, 1 } };
        var transparent = new[,] { { true, true } };
        var image = IcoBmpImage.Indexed(1, palette, indices, transparent);

        var rgba = new IcoBmp1Decoder().DecodeIcoBmpToRgba(image, Header(2, 1, 1, 2));

        Assert.Equal(_white, PixelAt(rgba, 2, 0, 0));
        Assert.Equal(0, AlphaAt(rgba, 2, 0, 0));
        Assert.Equal(_black, PixelAt(rgba, 2, 1, 0));
        Assert.Equal(255, AlphaAt(rgba, 2, 1, 0));
    }

    [Fact]
    public void Bmp4_SwapsThePaletteWhenEveryPixelIsMasked()
    {
        Rgb[] palette = [_black, _red, _green, _blue];
        var indices = new byte[,] { { 0, 1 } };
        var transparent = new[,] { { true, true } };
        var image = IcoBmpImage.Indexed(4, palette, indices, transparent);

        var rgba = new IcoBmp4Decoder().DecodeIcoBmpToRgba(image, Header(2, 1, 4, palette.Length));

        Assert.Equal(_red, PixelAt(rgba, 2, 0, 0));
        Assert.Equal(0, AlphaAt(rgba, 2, 0, 0));
        Assert.Equal(_black, PixelAt(rgba, 2, 1, 0));
        Assert.Equal(255, AlphaAt(rgba, 2, 1, 0));
    }

    [Fact]
    public void Bmp8_SwapsThePaletteWhenEveryPixelIsMasked()
    {
        Rgb[] palette = [_black, _red, _green];
        var indices = new byte[,] { { 0, 1 } };
        var transparent = new[,] { { true, true } };
        var image = IcoBmpImage.Indexed(8, palette, indices, transparent);

        var rgba = new IcoBmp8Decoder().DecodeIcoBmpToRgba(image, Header(2, 1, 8, palette.Length));

        Assert.Equal(_red, PixelAt(rgba, 2, 0, 0));
        Assert.Equal(0, AlphaAt(rgba, 2, 0, 0));
        Assert.Equal(_black, PixelAt(rgba, 2, 1, 0));
        Assert.Equal(255, AlphaAt(rgba, 2, 1, 0));
    }

    [Fact]
    public void Bmp4_UnpacksHighAndLowNibbles()
    {
        Rgb[] palette = new Rgb[16];
        palette[0] = _black;
        palette[1] = _red;
        palette[2] = _green;
        palette[15] = _blue;
        var indices = new byte[,] { { 1, 2, 15, 0 } };
        var image = IcoBmpImage.Indexed(4, palette, indices);

        var rgba = new IcoBmp4Decoder().DecodeIcoBmpToRgba(image, Header(4, 1, 4, 16));

        Assert.Equal(_red, PixelAt(rgba, 4, 0, 0));
        Assert.Equal(_green, PixelAt(rgba, 4, 1, 0));
        Assert.Equal(_blue, PixelAt(rgba, 4, 2, 0));
        Assert.Equal(_black, PixelAt(rgba, 4, 3, 0));
    }

    [Fact]
    public void Bmp4_HonoursTheAndMask()
    {
        Rgb[] palette = new Rgb[16];
        palette[1] = _red;
        var indices = new byte[,] { { 1, 1 }, { 1, 1 } };
        var transparent = new[,] { { false, true }, { false, false } };
        var image = IcoBmpImage.Indexed(4, palette, indices, transparent);

        var rgba = new IcoBmp4Decoder().DecodeIcoBmpToRgba(image, Header(2, 2, 4, 16));

        Assert.Equal(255, AlphaAt(rgba, 2, 0, 0));
        Assert.Equal(0, AlphaAt(rgba, 2, 1, 0));
        Assert.Equal(255, AlphaAt(rgba, 2, 1, 1));
    }

    [Fact]
    public void Bmp8_MapsPaletteIndicesAcrossPaddedRows()
    {
        // Three bytes per row means the decoder has to skip a byte of padding per row.
        Rgb[] palette = new Rgb[256];
        palette[1] = _red;
        palette[2] = _green;
        palette[3] = _blue;
        var indices = new byte[,] { { 1, 2, 3 }, { 3, 2, 1 } };
        var image = IcoBmpImage.Indexed(8, palette, indices);

        var rgba = new IcoBmp8Decoder().DecodeIcoBmpToRgba(image, Header(3, 2, 8, 256));

        Assert.Equal(_red, PixelAt(rgba, 3, 0, 0));
        Assert.Equal(_green, PixelAt(rgba, 3, 1, 0));
        Assert.Equal(_blue, PixelAt(rgba, 3, 2, 0));
        Assert.Equal(_blue, PixelAt(rgba, 3, 0, 1));
        Assert.Equal(_red, PixelAt(rgba, 3, 2, 1));
    }

    [Fact]
    public void Bmp24_SwapsBlueAndRed()
    {
        var pixels = new Rgb[,] { { _red, _green }, { _blue, _white } };
        var image = IcoBmpImage.TrueColor24(pixels);

        var rgba = new IcoBmp24Decoder().DecodeIcoBmpToRgba(image, Header(2, 2, 24, 0));

        Assert.Equal(_red, PixelAt(rgba, 2, 0, 0));
        Assert.Equal(_green, PixelAt(rgba, 2, 1, 0));
        Assert.Equal(_blue, PixelAt(rgba, 2, 0, 1));
        Assert.Equal(_white, PixelAt(rgba, 2, 1, 1));
    }

    [Fact]
    public void Bmp24_HandlesRowPadding()
    {
        // Three pixels are nine bytes per row, padded to twelve.
        var pixels = new Rgb[,] { { _red, _green, _blue } };
        var image = IcoBmpImage.TrueColor24(pixels);

        var rgba = new IcoBmp24Decoder().DecodeIcoBmpToRgba(image, Header(3, 1, 24, 0));

        Assert.Equal(_red, PixelAt(rgba, 3, 0, 0));
        Assert.Equal(_green, PixelAt(rgba, 3, 1, 0));
        Assert.Equal(_blue, PixelAt(rgba, 3, 2, 0));
    }

    [Fact]
    public void Bmp24_HonoursTheAndMask()
    {
        var pixels = new Rgb[,] { { _red, _green } };
        var transparent = new[,] { { false, true } };
        var image = IcoBmpImage.TrueColor24(pixels, transparent);

        var rgba = new IcoBmp24Decoder().DecodeIcoBmpToRgba(image, Header(2, 1, 24, 0));

        Assert.Equal(255, AlphaAt(rgba, 2, 0, 0));
        Assert.Equal(0, AlphaAt(rgba, 2, 1, 0));
    }

    [Fact]
    public void Bmp32_KeepsTheAlphaChannel()
    {
        var pixels = new (Rgb, byte)[,]
        {
            { (_red, 255), (_green, 128) },
            { (_blue, 0), (_white, 64) }
        };
        var image = IcoBmpImage.TrueColor32(pixels);

        var rgba = new IcoBmp32Decoder().DecodeIcoBmpToRgba(image, Header(2, 2, 32, 0));

        Assert.Equal(_red, PixelAt(rgba, 2, 0, 0));
        Assert.Equal(255, AlphaAt(rgba, 2, 0, 0));
        Assert.Equal(_green, PixelAt(rgba, 2, 1, 0));
        Assert.Equal(128, AlphaAt(rgba, 2, 1, 0));
        Assert.Equal(_blue, PixelAt(rgba, 2, 0, 1));
        Assert.Equal(0, AlphaAt(rgba, 2, 0, 1));
        Assert.Equal(64, AlphaAt(rgba, 2, 1, 1));
    }

    [Fact]
    public void Bmp4_LocatesPixelDataAfterATruncatedPalette()
    {
        // A 4 bit image may declare fewer than 16 colours; the pixel data then starts right after
        // the shorter palette. Assuming a full one reads 32 bytes past the pixels.
        Rgb[] palette = [_black, _red, _green, _blue];
        var indices = new byte[,] { { 1, 2, 3, 0 } };
        var image = IcoBmpImage.Indexed(4, palette, indices);

        var rgba = new IcoBmp4Decoder().DecodeIcoBmpToRgba(image, Header(4, 1, 4, palette.Length));

        Assert.Equal(_red, PixelAt(rgba, 4, 0, 0));
        Assert.Equal(_green, PixelAt(rgba, 4, 1, 0));
        Assert.Equal(_blue, PixelAt(rgba, 4, 2, 0));
        Assert.Equal(_black, PixelAt(rgba, 4, 3, 0));
    }

    [Fact]
    public void Bmp1_LocatesPixelDataAfterATruncatedPalette()
    {
        Rgb[] palette = [_red];
        var indices = new byte[,] { { 0, 0 } };
        var image = IcoBmpImage.Indexed(1, palette, indices);

        var rgba = new IcoBmp1Decoder().DecodeIcoBmpToRgba(image, Header(2, 1, 1, palette.Length));

        Assert.Equal(_red, PixelAt(rgba, 2, 0, 0));
        Assert.Equal(_red, PixelAt(rgba, 2, 1, 0));
    }

    [Fact]
    public void Bmp8_LocatesPixelDataAfterATruncatedPalette()
    {
        Rgb[] palette = [_black, _red, _green];
        var indices = new byte[,] { { 1, 2, 0 } };
        var image = IcoBmpImage.Indexed(8, palette, indices);

        var rgba = new IcoBmp8Decoder().DecodeIcoBmpToRgba(image, Header(3, 1, 8, palette.Length));

        Assert.Equal(_red, PixelAt(rgba, 3, 0, 0));
        Assert.Equal(_green, PixelAt(rgba, 3, 1, 0));
        Assert.Equal(_black, PixelAt(rgba, 3, 2, 0));
    }

    [Fact]
    public void Bmp24_SkipsADeclaredPaletteBeforeThePixelData()
    {
        // A true colour bitmap does not need a palette, but the format lets it declare one and the
        // pixel data then starts after it.
        Rgb[] palette = [_white, _black, _red, _green];
        var pixels = new Rgb[,] { { _red, _green } };
        var image = IcoBmpImage.TrueColor24(pixels, palette: palette);

        var rgba = new IcoBmp24Decoder().DecodeIcoBmpToRgba(image, Header(2, 1, 24, palette.Length));

        Assert.Equal(_red, PixelAt(rgba, 2, 0, 0));
        Assert.Equal(_green, PixelAt(rgba, 2, 1, 0));
    }

    [Fact]
    public void Bmp32_SkipsADeclaredPaletteBeforeThePixelData()
    {
        Rgb[] palette = [_white, _black, _red];
        var pixels = new (Rgb, byte)[,] { { (_red, 255), (_green, 128) } };
        var image = IcoBmpImage.TrueColor32(pixels, palette);

        var rgba = new IcoBmp32Decoder().DecodeIcoBmpToRgba(image, Header(2, 1, 32, palette.Length));

        Assert.Equal(_red, PixelAt(rgba, 2, 0, 0));
        Assert.Equal(255, AlphaAt(rgba, 2, 0, 0));
        Assert.Equal(_green, PixelAt(rgba, 2, 1, 0));
        Assert.Equal(128, AlphaAt(rgba, 2, 1, 0));
    }

    /// <summary>
    /// Some writers leave the AND mask out, and Windows then draws every pixel of the image.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(8)]
    public void Indexed_DrawsEveryPixelInItsOwnColourWithoutAMask(int bitCount)
    {
        Rgb[] palette = [_black, _green];
        var indices = new byte[,] { { 1, 1 }, { 1, 1 } };
        var image = WithoutMask(IcoBmpImage.Indexed(bitCount, palette, indices), width: 2, height: 2);

        var rgba = Decoder(bitCount).DecodeIcoBmpToRgba(image, Header(2, 2, (ushort)bitCount, palette.Length));

        Assert.All(new[] { (0, 0), (1, 0), (0, 1), (1, 1) }, point =>
        {
            Assert.Equal(_green, PixelAt(rgba, 2, point.Item1, point.Item2));
            Assert.Equal(255, AlphaAt(rgba, 2, point.Item1, point.Item2));
        });
    }

    [Fact]
    public void Bmp24_DrawsEveryPixelWithoutAMask()
    {
        var pixels = new Rgb[,] { { _red, _green }, { _blue, _white } };
        var image = WithoutMask(IcoBmpImage.TrueColor24(pixels), width: 2, height: 2);

        var rgba = new IcoBmp24Decoder().DecodeIcoBmpToRgba(image, Header(2, 2, 24, 0));

        Assert.All(new[] { (0, 0), (1, 0), (0, 1), (1, 1) }, point => Assert.Equal(255, AlphaAt(rgba, 2, point.Item1, point.Item2)));
    }

    /// <summary>
    /// Windows ignores a mask the data cuts short, even where the rows it does hold would hide pixels.
    /// </summary>
    [Fact]
    public void Bmp8_IgnoresAMaskTheDataCutsShort()
    {
        Rgb[] palette = [_black, _red];
        var indices = new byte[,] { { 1, 1 }, { 1, 1 } };
        var transparent = new[,] { { true, true }, { true, true } };
        var image = IcoBmpImage.Indexed(8, palette, indices, transparent);
        var cutShort = image.AsSpan(0, image.Length - 1).ToArray();

        var rgba = new IcoBmp8Decoder().DecodeIcoBmpToRgba(cutShort, Header(2, 2, 8, palette.Length));

        Assert.All(new[] { (0, 0), (1, 0), (0, 1), (1, 1) }, point =>
        {
            Assert.Equal(_red, PixelAt(rgba, 2, point.Item1, point.Item2));
            Assert.Equal(255, AlphaAt(rgba, 2, point.Item1, point.Item2));
        });
    }

    private static byte[] WithoutMask(byte[] image, int width, int height)
    {
        var maskSize = ((width + 31) / 32 * 4) * height;
        return image.AsSpan(0, image.Length - maskSize).ToArray();
    }

    private static IIcoBmpDecoder Decoder(int bitCount) => bitCount switch
    {
        1 => new IcoBmp1Decoder(),
        4 => new IcoBmp4Decoder(),
        8 => new IcoBmp8Decoder(),
        24 => new IcoBmp24Decoder(),
        _ => new IcoBmp32Decoder()
    };

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(24)]
    [InlineData(32)]
    public void BitCountSupported_MatchesTheDecoder(int bitCount)
        => Assert.Equal(bitCount, Decoder(bitCount).BitCountSupported);
}
