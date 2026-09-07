namespace Ico.Reader.PeDecoder.Models;

internal sealed record ResourceDirectory
{
    internal const int HeaderSize = 16;

    /// <summary>
    /// The resource tree nests type, then name, then language, so a well-formed file never goes
    /// deeper than a handful of levels. The limit stops a malformed or hostile file from recursing
    /// until the stack gives out.
    /// </summary>
    internal const int MaxDepth = 32;

    internal const string RootName = "Root";

    public string Name { get; init; } = string.Empty;
    public int Level { get; init; }
    public uint Characteristics { get; init; }
    public DateTime TimeDateStamp { get; init; }
    public ushort MajorVersion { get; init; }
    public ushort MinorVersion { get; init; }
    public ushort NumberOfNamedEntries { get; init; }
    public ushort NumberOfIdEntries { get; init; }

    public List<ResourceDirectory> Subdirectories { get; init; } = [];
    public List<ResourceDataEntry> DataEntries { get; init; } = [];

    /// <summary>
    /// The section the resource tree was read from, kept so that resolving a data entry to a file
    /// offset does not have to re-read the section table. Only set on the root directory.
    /// </summary>
    public SectionHeader? Section { get; init; }

    public override string ToString() => $"{Name} [DataEntries: {DataEntries.Count}] [Subdirectories: {Subdirectories.Count}]";

    public ResourceDirectory? GetDirectory(string directoryName)
    {
        if (Level != 1)
            return null;

        return Subdirectories.FirstOrDefault(x => string.Equals(x.Name, directoryName, StringComparison.OrdinalIgnoreCase));
    }

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
