namespace Ico.Reader.Test.Unit.Data;

public sealed class BmpInfoHeaderTests
{
    [Theory]
    [InlineData(1, 0, 2)]
    [InlineData(4, 0, 16)]
    [InlineData(8, 0, 256)]
    [InlineData(4, 8, 8)]
    [InlineData(8, 16, 16)]
    public void CalculatePaletteSize_UsesClrUsedWhenItFits(ushort bitCount, int clrUsed, int expected)
    {
        var header = new BmpInfoHeader { BitCount = bitCount, ClrUsed = clrUsed };

        Assert.Equal(expected, header.CalculatePaletteSize());
    }

    [Fact]
    public void CalculatePaletteSize_FallsBackToTheFullPaletteWhenClrUsedIsTooLarge()
    {
        var header = new BmpInfoHeader { BitCount = 4, ClrUsed = 999 };

        Assert.Equal(16, header.CalculatePaletteSize());
    }

    [Theory]
    [InlineData(24, 0, 0)]
    [InlineData(32, 0, 0)]
    [InlineData(32, 4, 4)]
    public void CalculatePaletteSize_OnlyCountsADeclaredPaletteAboveEightBits(ushort bitCount, int clrUsed, int expected)
    {
        var header = new BmpInfoHeader { BitCount = bitCount, ClrUsed = clrUsed };

        Assert.Equal(expected, header.CalculatePaletteSize());
    }

    [Theory]
    [InlineData(1, 48)]
    [InlineData(4, 104)]
    [InlineData(8, 1064)]
    public void CalculateDataOffset_SkipsHeaderAndFullPalette(ushort bitCount, int expected)
    {
        var header = new BmpInfoHeader { Size = 40, BitCount = bitCount };

        Assert.Equal(expected, header.CalculateDataOffset());
    }

    [Theory]
    [InlineData(1, 1, 44)]
    [InlineData(4, 4, 56)]
    [InlineData(4, 8, 72)]
    [InlineData(8, 16, 104)]
    public void CalculateDataOffset_SkipsOnlyTheDeclaredPalette(ushort bitCount, int clrUsed, int expected)
    {
        // A palette shorter than the bit depth allows is legal, and the pixel data starts right
        // after it rather than after a full one.
        var header = new BmpInfoHeader { Size = 40, BitCount = bitCount, ClrUsed = clrUsed };

        Assert.Equal(expected, header.CalculateDataOffset());
    }

    [Theory]
    [InlineData(24)]
    [InlineData(32)]
    public void CalculateDataOffset_SkipsNoPaletteForTrueColor(ushort bitCount)
    {
        var header = new BmpInfoHeader { Size = 40, BitCount = bitCount };

        Assert.Equal(40, header.CalculateDataOffset());
    }
}
