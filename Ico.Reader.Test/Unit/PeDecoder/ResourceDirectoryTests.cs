using PeDecoder.Models;

namespace Ico.Reader.Test.Unit.PeDecoder;

public sealed class ResourceDirectoryTests
{
    private static ResourceDirectory Root(params string[] subdirectoryNames) => new()
    {
        Name = "Root",
        Level = 1,
        Subdirectories = [.. subdirectoryNames.Select(name => new ResourceDirectory
        {
            Name = name,
            Level = 2,
            Subdirectories = [new ResourceDirectory { Name = "1033", Level = 3, DataEntries = [new ResourceDataEntry { ID = 1 }] }]
        })]
    };

    [Fact]
    public void GetDirectory_MatchesTheWholeNameRatherThanASubstring()
    {
        // A substring match would return RT_ICON_BACKUP, because it contains "RT_ICON".
        var root = Root("RT_ICON_BACKUP", "RT_ICON");

        var found = root.GetDirectory("RT_ICON");

        Assert.NotNull(found);
        Assert.Equal("RT_ICON", found.Name);
    }

    [Fact]
    public void GetResources_MatchesTheWholeNameRatherThanASubstring()
    {
        var root = Root("RT_ICON_BACKUP", "RT_ICON");
        root.Subdirectories[1].Subdirectories[0].DataEntries[0].ID = 42;

        var resources = root.GetResources("RT_ICON");

        Assert.NotNull(resources);
        Assert.Equal(42u, Assert.Single(resources).ID);
    }

    [Fact]
    public void GetDirectory_IgnoresCase()
    {
        var root = Root("RT_GROUP_ICON");

        Assert.NotNull(root.GetDirectory("rt_group_icon"));
    }

    [Fact]
    public void GetDirectory_ReturnsNullForAnUnknownName()
    {
        var root = Root("RT_ICON");

        Assert.Null(root.GetDirectory("RT_CURSOR"));
    }

    [Fact]
    public void GetDirectory_OnlyAnswersFromTheRootLevel()
    {
        var root = Root("RT_ICON");
        var typeDirectory = root.Subdirectories[0];

        Assert.Null(typeDirectory.GetDirectory("1033"));
    }

    [Fact]
    public void GetResources_SkipsLanguageDirectoriesWithoutData()
    {
        // A malformed file can leave a language subdirectory empty; indexing it blindly would throw.
        var root = Root("RT_ICON");
        root.Subdirectories[0].Subdirectories.Insert(0, new ResourceDirectory { Name = "1031", Level = 3 });

        var resources = root.GetResources("RT_ICON");

        Assert.NotNull(resources);
        Assert.Single(resources);
    }
}
