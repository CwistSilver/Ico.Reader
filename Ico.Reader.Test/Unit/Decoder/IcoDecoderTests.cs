using Ico.Reader.Decoder;
using Ico.Reader.Decoder.ImageDecoder;

namespace Ico.Reader.Test.Unit.Decoder;

public sealed class IcoDecoderTests
{
    private readonly IcoDecoder _decoder = new();

    private static byte[] PngHeader()
    {
        var data = new byte[26];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(data, 0);
        data[24] = 8;
        data[25] = 6;

        return data;
    }

    private static byte[] BmpHeader() => IcoBmpImage.Indexed(8, [new(1, 2, 3)], new byte[2, 2]);

    [Fact]
    public void ReadFormat_DetectsBmp() => Assert.Equal(IcoImageFormat.BMP, _decoder.ReadFormat(BmpHeader()));

    [Fact]
    public void ReadFormat_DetectsPng() => Assert.Equal(IcoImageFormat.PNG, _decoder.ReadFormat(PngHeader()));

    [Fact]
    public void ReadFormat_ThrowsForUnknownData()
    {
        var garbage = new byte[26];
        garbage[0] = 0xFF;

        Assert.Throws<NotSupportedException>(() => _decoder.ReadFormat(garbage));
    }

    [Fact]
    public void ReadImageMetadata_ReturnsNullForUnknownData()
    {
        var garbage = new byte[26];
        garbage[0] = 0xFF;

        Assert.Null(_decoder.ReadImageMetadata(garbage));
    }

    [Fact]
    public void GetImageData_DispatchesToTheMatchingDecoder()
    {
        var png = _decoder.GetImageData(PngHeader(), IcoImageFormat.PNG);

        Assert.Equal(PngHeader(), png);
    }

    [Fact]
    public void GetImageData_ThrowsWhenNoDecoderHandlesTheFormat()
    {
        var decoder = new IcoDecoder([new PngDecoder()]);

        var exception = Assert.Throws<NotSupportedException>(() => decoder.GetImageData(BmpHeader(), IcoImageFormat.BMP));
        Assert.Contains("BMP", exception.Message);
    }

    [Fact]
    public void Constructor_UsesTheSuppliedDecoders()
    {
        var decoder = new IcoDecoder([new PngDecoder()]);

        Assert.Equal(IcoImageFormat.PNG, decoder.ReadFormat(PngHeader()));
        Assert.Throws<NotSupportedException>(() => decoder.ReadFormat(BmpHeader()));
    }
}
