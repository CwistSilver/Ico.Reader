using System.Buffers.Binary;

using Ico.Reader.Decoder.ImageDecoder;

namespace Ico.Reader.Test.Unit.Decoder;

public sealed class PngDecoderTests
{
    private readonly PngDecoder _decoder = new();

    private static byte[] Ihdr(int width, int height, byte bitDepth, byte colorType)
    {
        var data = new byte[26];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(data, 0);
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(16, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(20, 4), height);
        data[24] = bitDepth;
        data[25] = colorType;

        return data;
    }

    [Fact]
    public void SupportedFormat_IsPng() => Assert.Equal(IcoImageFormat.PNG, _decoder.SupportedFormat);

    [Fact]
    public void IsSupported_AcceptsThePngSignature() => Assert.True(_decoder.IsSupported(Ihdr(16, 16, 8, 6)));

    [Fact]
    public void IsSupported_RejectsABmpInfoHeader()
    {
        var bmp = new byte[26];
        BitConverter.GetBytes(40).CopyTo(bmp, 0);

        Assert.False(_decoder.IsSupported(bmp));
    }

    [Fact]
    public void Decode_ReturnsTheDataUnchanged()
    {
        var data = Ihdr(8, 8, 8, 6);

        var decoded = _decoder.Decode(data);

        Assert.Equal(data, decoded);
        Assert.NotSame(data, decoded);
    }

    [Fact]
    public void ReadImageMetadata_ReadsBigEndianDimensions()
    {
        var metadata = _decoder.ReadImageMetadata(Ihdr(300, 150, 8, 6));

        Assert.NotNull(metadata);
        Assert.Equal(300, metadata.Width);
        Assert.Equal(150, metadata.Height);
        Assert.Equal(IcoImageFormat.PNG, metadata.Format);
    }

    [Theory]
    [InlineData(8, 0, 8)]    // greyscale
    [InlineData(16, 0, 16)]
    [InlineData(8, 2, 24)]   // truecolour
    [InlineData(8, 3, 8)]    // indexed
    [InlineData(4, 3, 4)]
    [InlineData(8, 4, 16)]   // greyscale + alpha
    [InlineData(8, 6, 32)]   // truecolour + alpha
    [InlineData(16, 6, 64)]
    public void ReadImageMetadata_DerivesBitCountFromDepthAndColorType(byte bitDepth, byte colorType, int expected)
    {
        var metadata = _decoder.ReadImageMetadata(Ihdr(16, 16, bitDepth, colorType));

        Assert.NotNull(metadata);
        Assert.Equal(expected, metadata.BitCount);
    }

    [Fact]
    public void ReadImageMetadata_ReportsZeroBitsForAnUnknownColorType()
    {
        var metadata = _decoder.ReadImageMetadata(Ihdr(16, 16, 8, 7));

        Assert.NotNull(metadata);
        Assert.Equal(0, metadata.BitCount);
    }
}
