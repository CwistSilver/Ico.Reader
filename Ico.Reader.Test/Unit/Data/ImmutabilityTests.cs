using System.Reflection;

namespace Ico.Reader.Test.Unit.Data;

/// <summary>
/// The image models are set once at construction. A settable property would let a caller change an
/// <see cref="ImageReference"/> after the decoder handed it out, which the concurrency guarantees
/// depend on not happening.
/// </summary>
public sealed class ImmutabilityTests
{
    /// <summary>An init-only setter is compiled as a normal setter carrying this required modifier.</summary>
    private static bool IsInitOnly(PropertyInfo property)
        => property.SetMethod?.ReturnParameter.GetRequiredCustomModifiers()
            .Any(x => x.FullName == "System.Runtime.CompilerServices.IsExternalInit") == true;

    public static TheoryData<Type> ImmutableModels =>
    [
        typeof(ImageReference),
        typeof(IconDirectoryEntry),
        typeof(CursorDirectoryEntry),
        typeof(IcoHeader),
        typeof(BmpInfoHeader),
        typeof(IconGroup),
        typeof(CursorGroup),
        typeof(DecodedIcoResult),
    ];

    [Theory]
    [MemberData(nameof(ImmutableModels))]
    public void ModelPropertiesAreInitOnly(Type modelType)
    {
        var settable = modelType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.SetMethod is not null && !IsInitOnly(x))
            .Select(x => x.Name)
            .ToArray();

        Assert.Empty(settable);
    }

    [Fact]
    public void ImageReference_CopiesCarryEveryOtherValueOver()
    {
        var reader = new IcoReader();
        var ico = reader.Read(TestFiles.Ico("icon_multi_mixed.ico"));
        Assert.NotNull(ico);

        // Every reference reached the caller through at least one With-style copy, so a copy that
        // dropped a field would show up as a zeroed value here.
        Assert.All(ico.ImageReferences, reference =>
        {
            Assert.NotEqual(0u, reference.Offset);
            Assert.NotEqual(0u, reference.Size);
            Assert.NotEqual(0, reference.Width);
            Assert.NotEqual(0, reference.Height);
            Assert.NotEqual(0, reference.BitCount);
        });

        Assert.Equal([0, 1, 2], ico.ImageReferences.Select(x => x.Id));
    }

    [Fact]
    public void ImageReference_CursorCopiesKeepTheirLocationAndSize()
    {
        var reader = new IcoReader();
        var cur = reader.Read(TestFiles.Cur("cursor_multi_mixed.cur"));
        Assert.NotNull(cur);

        Assert.All(cur.ImageReferences, reference =>
        {
            Assert.Equal(IcoType.Cursor, reference.IcoType);
            Assert.NotEqual(0u, reference.Offset);
            Assert.NotEqual(0u, reference.Size);
            Assert.NotEqual(0, reference.Width);
        });
    }

    [Fact]
    public void PeEntries_KeepEveryFieldWhenResolvedToAFileOffset()
    {
        // The PE path rebuilds each entry to attach the resolved offset, so a copy that lost a field
        // would surface as a zeroed dimension or size here.
        var pe = new IcoReader().Read(TestFiles.PeFixture);
        Assert.NotNull(pe);

        foreach (var group in pe.IconGroups)
        {
            Assert.All(group.DirectoryEntries, entry =>
            {
                Assert.NotEqual(0u, entry.RealImageOffset);
                Assert.NotEqual(0u, entry.ImageSize);
                Assert.NotEqual(0, entry.ColorDepth);
            });
        }

        foreach (var group in pe.CursorGroups)
        {
            Assert.All(group.DirectoryEntries, entry =>
            {
                Assert.NotEqual(0u, entry.RealImageOffset);
                Assert.NotEqual(0u, entry.ImageSize);
                Assert.NotEqual(0, entry.Width);
            });
        }
    }
}
