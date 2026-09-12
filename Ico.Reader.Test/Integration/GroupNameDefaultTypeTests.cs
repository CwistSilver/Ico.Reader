namespace Ico.Reader.Test.Integration;

/// <summary>
/// The overloads that take a group name default their <see cref="IcoType"/> to
/// <see cref="IcoType.Icon"/>, so naming an icon group does not have to spell the type out.
/// </summary>
public sealed class GroupNameDefaultTypeTests
{
    private const string Fixture = "icon_multi.ico";
    private const string Group = "1";

    private readonly IcoReader _reader = new();

    private IcoData Read()
    {
        var ico = _reader.Read(TestFiles.Ico(Fixture));
        Assert.NotNull(ico);
        return ico;
    }

    [Fact]
    public void GetGroup_DefaultsToIcon()
    {
        var ico = Read();

        Assert.Same(ico.GetGroup(Group, IcoType.Icon), ico.GetGroup(Group));
    }

    [Fact]
    public void GetImage_DefaultsToIcon()
    {
        var ico = Read();

        Assert.Equal(ico.GetImage(Group, 0, IcoType.Icon), ico.GetImage(Group, 0));
    }

    [Fact]
    public async Task GetImageAsync_DefaultsToIcon()
    {
        var ico = Read();

        var expected = await ico.GetImageAsync(Group, 0, IcoType.Icon, TestContext.Current.CancellationToken);
        var actual = await ico.GetImageAsync(Group, 0, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetImageReference_DefaultsToIcon()
    {
        var ico = Read();

        Assert.Equal(ico.GetImageReference(Group, 0, IcoType.Icon), ico.GetImageReference(Group, 0));
    }

    [Fact]
    public void GetImageReferences_DefaultsToIcon()
    {
        var ico = Read();

        Assert.Equal(ico.GetImageReferences(Group, IcoType.Icon), ico.GetImageReferences(Group));
    }

    [Fact]
    public void PreferredImageIndex_DefaultsToIcon()
    {
        var ico = Read();

        Assert.Equal(ico.PreferredImageIndex(Group, IcoType.Icon), ico.PreferredImageIndex(Group));
    }

    [Fact]
    public void CursorGroupsStillNeedTheTypeSpelledOut()
    {
        var cur = _reader.Read(TestFiles.Cur("cursor_multi.cur"));
        Assert.NotNull(cur);

        var group = cur.CursorGroups[0];

        Assert.Same(group, cur.GetGroup(group.Name, IcoType.Cursor));
        Assert.Throws<InvalidOperationException>(() => cur.GetGroup(group.Name));
    }
}
