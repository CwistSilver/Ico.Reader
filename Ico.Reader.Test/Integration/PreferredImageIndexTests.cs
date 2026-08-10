namespace Ico.Reader.Test.Integration;

/// <summary>
/// icon_multi_mixed.ico holds 16px/4bpp, 32px/8bpp and 48px/32bpp entries, so area and colour
/// depth both rank in the same direction and the weights decide the winner.
/// </summary>
public sealed class PreferredImageIndexTests
{
    private readonly IcoReader _reader = new();

    private IcoData Read(string file)
    {
        var ico = _reader.Read(TestFiles.Ico(file));
        Assert.NotNull(ico);
        return ico;
    }

    [Fact]
    public void PreferredImageIndex_PicksTheLargestByDefault()
    {
        var ico = Read("icon_multi_mixed.ico");

        var index = ico.PreferredImageIndex();

        Assert.Equal(48, ico.ImageReferences[index].Width);
    }

    [Fact]
    public void PreferredImageIndex_IgnoresColorDepthWhenItsWeightIsZero()
    {
        var ico = Read("icon_multi_mixed.ico");

        var index = ico.PreferredImageIndex(colorBitWeight: 0f, areaWeight: 1f);

        Assert.Equal(48, ico.ImageReferences[index].Width);
    }

    [Fact]
    public void PreferredImageIndex_IgnoresAreaWhenItsWeightIsZero()
    {
        var ico = Read("icon_multi_mixed.ico");

        var index = ico.PreferredImageIndex(colorBitWeight: 1f, areaWeight: 0f);

        Assert.Equal(32, ico.ImageReferences[index].BitCount);
    }

    [Fact]
    public void PreferredImageIndex_TreatsWeightsAsARatio()
    {
        var ico = Read("icon_multi_mixed.ico");

        Assert.Equal(ico.PreferredImageIndex(1f, 2f), ico.PreferredImageIndex(0.333f, 0.667f));
        Assert.Equal(ico.PreferredImageIndex(2f, 1f), ico.PreferredImageIndex(20f, 10f));
    }

    [Fact]
    public void PreferredImageIndex_ScopedToAGroupReturnsAGlobalIndex()
    {
        var ico = Read("icon_multi.ico");

        var index = ico.PreferredImageIndex(ico.Groups[0]);

        Assert.InRange(index, 0, ico.ImageReferences.Count - 1);
        Assert.Equal(48, ico.ImageReferences[index].Width);
        Assert.Equal(index, ico.PreferredImageIndex("1", IcoType.Icon));
    }

    [Fact]
    public void PreferredImageIndex_WorksForASingleImage()
    {
        var ico = Read("icon_32_8bpp.ico");

        Assert.Equal(0, ico.PreferredImageIndex());
    }

    [Theory]
    [InlineData(-1f, 1f)]
    [InlineData(1f, -1f)]
    public void PreferredImageIndex_RejectsNegativeWeights(float colorBitWeight, float areaWeight)
    {
        var ico = Read("icon_multi.ico");

        Assert.Throws<ArgumentOutOfRangeException>(() => ico.PreferredImageIndex(colorBitWeight, areaWeight));
    }

    [Fact]
    public void PreferredImageIndex_RejectsTwoZeroWeights()
    {
        var ico = Read("icon_multi.ico");

        Assert.Throws<ArgumentException>(() => ico.PreferredImageIndex(0f, 0f));
    }
}
