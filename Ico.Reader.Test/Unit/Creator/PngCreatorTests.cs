using System.Text;

using Ico.Reader.Creator;

namespace Ico.Reader.Test.Unit.Creator;

public sealed class PngCreatorTests
{
    private readonly PngCreator _creator = new();

    private static BMP_Info_Header Header(int width, int height) => new()
    {
        Size = 40,
        Width = width,
        // ICO stores the image and its mask stacked, so the header height is doubled.
        Height = height * 2,
        Planes = 1,
        BitCount = 32
    };

    private static byte[] Gradient(int width, int height)
    {
        var rgba = new byte[width * height * 4];
        for (var i = 0; i < rgba.Length; i++)
            rgba[i] = (byte)(i * 7 % 251);

        return rgba;
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 5)]
    [InlineData(16, 16)]
    [InlineData(37, 11)]
    public void CreatePng_ProducesAValidContainer(int width, int height)
    {
        var png = _creator.CreatePng(Gradient(width, height), Header(width, height));

        var image = PngImage.Parse(png);

        Assert.Equal(width, image.Width);
        Assert.Equal(height, image.Height);
        Assert.Equal(8, image.BitDepth);
        Assert.Equal(6, image.ColorType);
        Assert.Equal(0, image.InterlaceMethod);
        Assert.Equal(["IHDR", "IDAT", "IEND"], image.ChunkTypes);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(4, 3)]
    [InlineData(37, 11)]
    public void CreatePng_RoundTripsThePixelsUnchanged(int width, int height)
    {
        var rgba = Gradient(width, height);

        var png = _creator.CreatePng(rgba, Header(width, height));

        Assert.Equal(rgba, PngImage.Parse(png).Rgba);
    }

    [Fact]
    public void CreatePng_UsesTheHalvedHeaderHeight()
    {
        var header = Header(8, 4);

        var image = PngImage.Parse(_creator.CreatePng(Gradient(8, 4), header));

        Assert.Equal(8, header.Width);
        Assert.Equal(8, header.Height);
        Assert.Equal(4, image.Height);
    }

    [Fact]
    public void CalculateCrc32_MatchesKnownVectors()
    {
        Assert.Equal(0x00000000u, PngCreator.CalculateCrc32([]));
        Assert.Equal(0xCBF43926u, PngCreator.CalculateCrc32(Encoding.ASCII.GetBytes("123456789")));
        Assert.Equal(0x414FA339u, PngCreator.CalculateCrc32(Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog")));
    }

    [Fact]
    public void CreatePng_WritesTheIendChunkWithoutPayload()
    {
        var png = _creator.CreatePng(Gradient(2, 2), Header(2, 2));

        var iend = png.AsSpan(png.Length - 12);
        Assert.Equal(0, System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(iend.Slice(0, 4)));
        Assert.Equal("IEND", Encoding.ASCII.GetString(iend.Slice(4, 4).ToArray()));
    }
}
