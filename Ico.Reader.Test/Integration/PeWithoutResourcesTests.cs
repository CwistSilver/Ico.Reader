using Ico.Reader.PeDecoder;

namespace Ico.Reader.Test.Integration;

/// <summary>
/// Plenty of real executables and DLLs carry no resources, so their optional header leaves the resource table empty.
/// Such a file holds no icons; it is not unreadable.
/// </summary>
public sealed class PeWithoutResourcesTests
{
    private static byte[] FixtureWithoutResources()
    {
        var pe = File.ReadAllBytes(TestFiles.PeFixture);
        Array.Clear(pe, PeResources.ResourceTableOffset(pe), 8);
        return pe;
    }

    [Fact]
    public void Read_ReturnsAResultWithoutGroupsOrImages()
    {
        var ico = new IcoReader().Read(FixtureWithoutResources());

        Assert.NotNull(ico);
        Assert.Equal(IcoOriginFileType.Dll, ico.OriginFileType);
        Assert.Empty(ico.Groups);
        Assert.Empty(ico.ImageReferences);
    }

    [Fact]
    public void DecodeResourceDirectory_ReturnsNull()
    {
        using var stream = new MemoryStream(FixtureWithoutResources());
        var decoder = new PeFileDecoder();

        Assert.Null(decoder.DecodeResourceDirectory(stream, decoder.DecodePE(stream)));
    }
}
