namespace Ico.Reader.PeDecoder.Models;

/// <summary>
/// One node of the resource tree. The tree nests by type, then by resource, then by language: the
/// root holds a directory per resource type, each of those holds a directory per resource, and those
/// hold a <see cref="ResourceDataEntry"/> per language.
/// </summary>
public sealed record ResourceDirectory
{
    internal const int HeaderSize = 16;

    /// <summary>
    /// The resource tree nests type, then name, then language, so a well-formed file never goes
    /// deeper than a handful of levels. The limit stops a malformed or hostile file from recursing
    /// until the stack gives out.
    /// </summary>
    internal const int MaxDepth = 32;

    internal const string RootName = "Root";

    /// <summary>
    /// Name of this directory: <c>Root</c> for the root, the <see cref="ResourceType"/> name for a
    /// resource type directory, and the resource's number or string name for a resource directory.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Depth of this directory in the tree: 1 for the root, 2 for a resource type and 3 for a single
    /// resource.
    /// </summary>
    public int Level { get; init; }

    /// <summary>
    /// Reserved and required to be zero.
    /// </summary>
    public uint Characteristics { get; init; }

    /// <summary>
    /// When the resource data was created, as recorded by the resource compiler.
    /// </summary>
    public DateTime TimeDateStamp { get; init; }

    /// <summary>
    /// Major version of the resource data.
    /// </summary>
    public ushort MajorVersion { get; init; }

    /// <summary>
    /// Minor version of the resource data.
    /// </summary>
    public ushort MinorVersion { get; init; }

    /// <summary>
    /// Number of entries in this directory that are identified by name.
    /// </summary>
    public ushort NumberOfNamedEntries { get; init; }

    /// <summary>
    /// Number of entries in this directory that are identified by number.
    /// </summary>
    public ushort NumberOfIdEntries { get; init; }

    /// <summary>
    /// The directories nested under this one.
    /// </summary>
    public List<ResourceDirectory> Subdirectories { get; init; } = [];

    /// <summary>
    /// The resources held directly by this directory.
    /// </summary>
    public List<ResourceDataEntry> DataEntries { get; init; } = [];

    /// <summary>
    /// The section the resource tree was read from, kept so that resolving a data entry to a file
    /// offset does not have to re-read the section table. Only set on the root directory.
    /// </summary>
    public SectionHeader? Section { get; init; }

    /// <inheritdoc />
    public override string ToString() => $"{Name} [DataEntries: {DataEntries.Count}] [Subdirectories: {Subdirectories.Count}]";

    /// <summary>
    /// Finds the directory holding every resource of one type. Only the root directory can answer
    /// this.
    /// </summary>
    /// <param name="directoryName">
    /// The resource type name, normally a <see cref="ResourceType"/> name such as <c>RT_GROUP_ICON</c>.
    /// </param>
    /// <returns>
    /// The directory, or <see langword="null"/> if this is not the root or the image holds no
    /// resources of that type.
    /// </returns>
    public ResourceDirectory? GetDirectory(string directoryName)
    {
        if (Level != 1)
            return null;

        return Subdirectories.FirstOrDefault(x => string.Equals(x.Name, directoryName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Collects every resource of one type, taking the entry for the first language each resource is
    /// stored in. Only the root directory can answer this.
    /// </summary>
    /// <param name="directoryName">
    /// The resource type name, normally a <see cref="ResourceType"/> name such as <c>RT_ANICURSOR</c>.
    /// </param>
    /// <returns>
    /// The resources, or <see langword="null"/> if this is not the root or the image holds no
    /// resources of that type. Resolve each one to a file offset with
    /// <see cref="ResourceDataEntry.GetFileOffset"/> and <see cref="Section"/>.
    /// </returns>
    public ResourceDataEntry[]? GetResources(string directoryName)
    {
        var foundDirectory = GetDirectory(directoryName);
        if (foundDirectory is null)
            return null;

        // A resource holds one data entry per language, but a malformed file can leave one with
        // none, so those are skipped rather than indexed into blindly.
        return [.. foundDirectory.Subdirectories.Where(x => x.DataEntries.Count > 0).Select(x => x.DataEntries[0])];
    }
}
