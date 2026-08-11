using System.Runtime.InteropServices;

namespace PeDecoder.Models;

internal sealed class ResourceDirectory
{
    internal const int HeaderSize = 16;

    /// <summary>
    /// The resource tree nests type, then name, then language, so a well-formed file never goes
    /// deeper than a handful of levels. The limit stops a malformed or hostile file from recursing
    /// until the stack gives out.
    /// </summary>
    private const int MaxDepth = 32;

    private const string RootName = "Root";

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

    public static ResourceDirectory? ReadFromStream(Stream stream, PeHeader peHeader)
    {
        if (peHeader.Optional is null || peHeader.Optional.ResourceTable is null)
            return null;

        var sectionHeaders = SectionHeader.ReadFromStream(stream, peHeader);

        var rsrcSection = peHeader.Optional.ResourceTable.FindFileSectionHeader(sectionHeaders);
        long resourceTableOffset = rsrcSection.GetFileOffset(peHeader.Optional.ResourceTable.VirtualAddress);

        var rootResourceDirectory = ReadResourceDirectory(stream, resourceTableOffset, rsrcSection, [], 1);
        rootResourceDirectory.Name = RootName;
        rootResourceDirectory.Section = rsrcSection;

        return rootResourceDirectory;
    }

    private static ResourceDirectory ReadResourceDirectory(Stream stream, long virtualAddress, SectionHeader rsrcSection, HashSet<long> visited, int level)
    {
        stream.Position = virtualAddress;

        var resourceDirectory = ReadResourceDirectoryBase(stream, virtualAddress, level);
        if (level >= MaxDepth || !visited.Add(virtualAddress))
            return resourceDirectory;

        var resourceDirectoryEntries = ResourceDirectoryEntry.ReadFromStream(stream, resourceDirectory, virtualAddress);

        foreach (var entry in resourceDirectoryEntries)
            ProcessResourceDirectoryEntry(stream, resourceDirectory, entry, rsrcSection, visited);

        return resourceDirectory;
    }

    private static ResourceDirectory ReadResourceDirectoryBase(Stream stream, long virtualAddress, int level)
    {
        Span<byte> resourceDirectoryBytes = stackalloc byte[16];
        stream.Read(resourceDirectoryBytes);

        ReadOnlySpan<byte> resourceDirectorySpan = resourceDirectoryBytes;

        var resourceDirectory = new ResourceDirectory
        {
            Characteristics = MemoryMarshal.Read<uint>(resourceDirectorySpan.Slice(0, 4)),
            TimeDateStamp = DateTimeOffset.FromUnixTimeSeconds(MemoryMarshal.Read<uint>(resourceDirectorySpan.Slice(4, 4))).UtcDateTime,
            MajorVersion = MemoryMarshal.Read<ushort>(resourceDirectorySpan.Slice(8, 2)),
            MinorVersion = MemoryMarshal.Read<ushort>(resourceDirectorySpan.Slice(10, 2)),
            NumberOfNamedEntries = MemoryMarshal.Read<ushort>(resourceDirectorySpan.Slice(12, 2)),
            NumberOfIdEntries = MemoryMarshal.Read<ushort>(resourceDirectorySpan.Slice(14, 2)),
            Level = level
        };

        return resourceDirectory;
    }

    private static void ProcessResourceDirectoryEntry(Stream stream, ResourceDirectory directory, ResourceDirectoryEntry entry, SectionHeader rsrcSection, HashSet<long> visited)
    {
        if (entry.SubdirectoryOffset != 0)
        {
            var newAddress = rsrcSection.PointerToRawData + entry.SubdirectoryOffset;
            var subResourceDirectory = ReadResourceDirectory(stream, newAddress, rsrcSection, visited, directory.Level + 1);
            SetName(stream, subResourceDirectory, entry, rsrcSection);

            directory.Subdirectories.Add(subResourceDirectory);
        }
        else
        {
            var dataEntry = ResourceDataEntry.ReadFromStream(stream, rsrcSection.PointerToRawData, entry.DataEntryOffset);
            directory.DataEntries.Add(dataEntry);
        }
    }

    private static void SetName(Stream stream, ResourceDirectory directory, ResourceDirectoryEntry entry, SectionHeader rsrcSection)
    {
        if (entry.NameOffset != 0)
        {
            directory.Name = entry.DecodeName(stream, rsrcSection.PointerToRawData);
        }
        else if (directory.Level == 2)
        {
            directory.Name = ((ResourceType)entry.IntegerID).ToString();
        }
        else
        {
            directory.Name = entry.IntegerID.ToString();
            for (var i = 0; i < directory.DataEntries.Count; i++)
                directory.DataEntries[i].ID = entry.IntegerID;
        }

    }
}
