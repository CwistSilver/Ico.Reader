namespace Ico.Reader.PeDecoder.Models;

/// <summary>
/// One node of the resource tree. The tree nests by type, then by name, then by language, so the
/// root holds a directory per <see cref="ResourceType"/> and the leaves hold
/// <see cref="ResourceDataEntry"/> values.
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
    /// Name of this directory. At the first level this is the <see cref="ResourceType"/> name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Depth of this directory in the tree, counting the root as zero.
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
    /// Finds a first level directory by name, which is where resources are grouped by type.
    /// </summary>
    /// <param name="directoryName">
    /// The directory name, normally a <see cref="ResourceType"/> name such as <c>RT_GROUP_ICON</c>.
    /// </param>
    /// <returns>
    /// The directory, or <see langword="null"/> if this is not the first level or no directory
    /// carries that name.
    /// </returns>
    public ResourceDirectory? GetDirectory(string directoryName)
    {
        if (Level != 1)
            return null;

        return Subdirectories.FirstOrDefault(x => string.Equals(x.Name, directoryName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Collects every resource held under a first level directory, taking one entry per language
    /// subdirectory.
    /// </summary>
    /// <param name="directoryName">
    /// The directory name, normally a <see cref="ResourceType"/> name such as <c>RT_ANICURSOR</c>.
    /// </param>
    /// <returns>
    /// The resources, or <see langword="null"/> if no directory carries that name. Resolve each one
    /// to a file offset with <see cref="ResourceDataEntry.GetFileOffset"/> and <see cref="Section"/>.
    /// </returns>
    public ResourceDataEntry[]? GetResources(string directoryName)
    {
        var foundDirectory = GetDirectory(directoryName);
        if (foundDirectory is null)
            return null;

        // A language subdirectory always carries exactly one data entry, but a malformed file can
        // leave one empty, so those are skipped rather than indexed into blindly.
        return [.. foundDirectory.Subdirectories.Where(x => x.DataEntries.Count > 0).Select(x => x.DataEntries[0])];
    }
}
