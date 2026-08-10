namespace Ico.Reader.Test.Integration;

public sealed class GroupAccessTests
{
    private const string Fixture = "icon_multi_mixed.ico";

    private readonly IcoReader _reader = new();

    private IcoData Read(string file = Fixture)
    {
        var ico = _reader.Read(TestFiles.Ico(file));
        Assert.NotNull(ico);
        return ico;
    }

    [Fact]
    public void GetGroup_FindsTheDefaultGroup()
    {
        var ico = Read();

        var group = ico.GetGroup("1", IcoType.Icon);

        Assert.Equal("1", group.Name);
        Assert.Same(group, ico.GetIconGroup("1"));
    }

    [Fact]
    public void GetGroup_ThrowsForAnUnknownName()
    {
        var ico = Read();

        Assert.Throws<InvalidOperationException>(() => ico.GetGroup("nope", IcoType.Icon));
        Assert.Throws<InvalidOperationException>(() => ico.GetIconGroup("nope"));
        Assert.Throws<InvalidOperationException>(() => ico.GetCursorGroup("1"));
    }

    [Fact]
    public void GetGroup_ThrowsWhenTheTypeDoesNotMatch()
    {
        var ico = Read();

        Assert.Throws<InvalidOperationException>(() => ico.GetGroup("1", IcoType.Cursor));
    }

    [Fact]
    public void GetGroups_ReturnsEveryGroupWithTheName()
    {
        var ico = Read();

        Assert.Single(ico.GetGroups("1"));
        Assert.Empty(ico.GetGroups("2"));
    }

    [Fact]
    public void GetImageReferences_ReturnsTheGroupsEntriesInOrder()
    {
        var ico = Read();

        var references = ico.GetImageReferences(ico.Groups[0]);

        Assert.Equal([16, 32, 48], references.Select(x => x.Width));
        Assert.Equal([4, 8, 32], references.Select(x => x.BitCount));
    }

    [Fact]
    public void GetImageReference_ResolvesByGroupPosition()
    {
        var ico = Read();
        var group = ico.Groups[0];

        Assert.Equal(16, ico.GetImageReference(group, 0).Width);
        Assert.Equal(32, ico.GetImageReference(group, 1).Width);
        Assert.Equal(48, ico.GetImageReference(group, 2).Width);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(int.MaxValue)]
    public void GetImageReference_RejectsAnIndexOutsideTheGroup(int index)
    {
        var ico = Read();

        Assert.Throws<ArgumentOutOfRangeException>(() => ico.GetImageReference(ico.Groups[0], index));
    }

    [Fact]
    public void GetImage_ByGroupMatchesGetImageByIndex()
    {
        var ico = Read();
        var group = ico.Groups[0];

        for (var i = 0; i < group.Size; i++)
            Assert.Equal(ico.GetImage(i), ico.GetImage(group, i));
    }

    [Fact]
    public void GetImage_ByGroupNameMatchesGetImageByIndex()
    {
        var ico = Read();

        Assert.Equal(ico.GetImage(0), ico.GetImage("1", 0, IcoType.Icon));
    }

    [Fact]
    public async Task GetImageAsync_ByGroupMatchesTheSynchronousResult()
    {
        var ico = Read();

        Assert.Equal(ico.GetImage(1), await ico.GetImageAsync(ico.Groups[0], 1));
        Assert.Equal(ico.GetImage(1), await ico.GetImageAsync("1", 1, IcoType.Icon));
    }
}
