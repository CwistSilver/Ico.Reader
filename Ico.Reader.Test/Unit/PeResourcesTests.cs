using Ico.Reader.PeDecoder;
using Ico.Reader.PeDecoder.Models;

namespace Ico.Reader.Test.Unit;

/// <summary>
/// Positive controls for <see cref="PeResources"/>, so a test that patches a PE file with it fails because of the
/// decoder and not because the patch landed in the wrong place.
/// </summary>
public sealed class PeResourcesTests
{
    public static TheoryData<ResourceType> ImageResourceTypes =>
        [ResourceType.RT_ICON, ResourceType.RT_CURSOR, ResourceType.RT_GROUP_ICON, ResourceType.RT_GROUP_CURSOR];

    [Theory]
    [MemberData(nameof(ImageResourceTypes))]
    public void Leaves_MatchTheResourcesTheDecoderReadsFromTheUntouchedFixture(ResourceType type)
    {
        var pe = File.ReadAllBytes(TestFiles.PeFixture);
        using var stream = new MemoryStream(pe);
        var decoder = new PeFileDecoder();
        var root = decoder.DecodeResourceDirectory(stream, decoder.DecodePE(stream));

        Assert.NotNull(root);
        Assert.Equal(
            root.GetResources(type.ToString())!.Select(entry => (entry.ID, entry.DataRVA, entry.Size)),
            PeResources.LeavesOf(pe, type).Select(leaf => (leaf.Id, leaf.DataRva, leaf.Size)));
    }

    [Fact]
    public void DataOffset_PointsAtTheImageData()
    {
        var pe = File.ReadAllBytes(TestFiles.PeFixture);

        Assert.All(PeResources.LeavesOf(pe, ResourceType.RT_ICON), leaf =>
        {
            var offset = PeResources.DataOffset(pe, leaf);
            Assert.True(BitConverter.ToInt32(pe, offset) == 40 || pe[offset] == 0x89, $"RT_ICON {leaf.Id} does not start with an image header.");
        });
    }
}
