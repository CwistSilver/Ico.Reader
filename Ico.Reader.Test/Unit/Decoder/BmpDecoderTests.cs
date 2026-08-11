using Ico.Reader.Decoder.ImageDecoder.Bmp;

namespace Ico.Reader.Test.Unit.Decoder;

public sealed class BmpDecoderTests
{
    private readonly BmpDecoder _decoder = new();

    private static readonly Rgb[] _palette = [new(0, 0, 0), new(255, 255, 255)];

    [Fact]
    public void SupportedFormat_IsBmp() => Assert.Equal(IcoImageFormat.Bmp, _decoder.SupportedFormat);

    [Fact]
    public void IsSupported_AcceptsAFortyByteInfoHeader()
        => Assert.True(_decoder.IsSupported(IcoBmpImage.Indexed(1, _palette, new byte[2, 2])));

    [Fact]
    public void IsSupported_RejectsAPngSignature()
    {
        var png = new byte[26];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(png, 0);

        Assert.False(_decoder.IsSupported(png));
    }

    [Fact]
    public void ReadImageMetadata_HalvesTheStackedHeight()
    {
        var image = IcoBmpImage.Indexed(8, [new(1, 2, 3)], new byte[6, 4]);

        var metadata = _decoder.ReadImageMetadata(image);

        Assert.NotNull(metadata);
        Assert.Equal(4, metadata.Width);
        Assert.Equal(6, metadata.Height);
        Assert.Equal(8, metadata.BitCount);
        Assert.Equal(IcoImageFormat.Bmp, metadata.Format);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(8)]
    public void Decode_ProducesAPngForEveryIndexedBitDepth(int bitCount)
    {
        var image = IcoBmpImage.Indexed(bitCount, _palette, new byte[2, 2]);

        var png = PngImage.Parse(_decoder.Decode(image));

        Assert.Equal(2, png.Width);
        Assert.Equal(2, png.Height);
    }

    [Fact]
    public void Decode_ThrowsForAnUnsupportedBitDepth()
    {
        var image = IcoBmpImage.Indexed(8, _palette, new byte[2, 2]);
        BitConverter.GetBytes((ushort)16).CopyTo(image, 14);

        var exception = Assert.Throws<NotSupportedException>(() => _decoder.Decode(image));
        Assert.Contains("16", exception.Message);
    }

    [Fact]
    public void Decode_ThrowsForCompressedData()
    {
        var image = IcoBmpImage.Indexed(8, _palette, new byte[2, 2]);
        BitConverter.GetBytes(1).CopyTo(image, 16);

        var exception = Assert.Throws<NotSupportedException>(() => _decoder.Decode(image));
        Assert.Contains("Compressed", exception.Message);
    }
}
