namespace PeDecoder.Models;

internal sealed class ResourceDirectory
{
    internal const int HeaderSize = 16;

    /// <summary>
    /// The resource tree nests type, then name, then language, so a well-formed file never goes
    /// deeper than a handful of levels. The limit stops a malformed or hostile file from recursing
    /// until the stack gives out.
    /// </summary>
    internal const int MaxDepth = 32;

    internal const string RootName = "Root";

    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public uint Characteristics { get; set; }
    public DateTime TimeDateStamp { get; set; }
    public ushort MajorVersion { get; set; }
    public ushort MinorVersion { get; set; }
    public ushort NumberOfNamedEntries { get; set; }
    public ushort NumberOfIdEntries { get; set; }

    public List<ResourceDirectory> Subdirectories { get; set; } = [];
    public List<ResourceDataEntry> DataEntries { get; set; } = [];

    /// <summary>
    /// The section the resource tree was read from, kept so that resolving a data entry to a file
    /// offset does not have to re-read the section table. Only set on the root directory.
    /// </summary>
    public SectionHeader? Section { get; set; }

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
