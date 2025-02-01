using System.Runtime.InteropServices;

namespace Ico.Reader.Data.Exe;
internal class ResourceDirectory
{
    internal string Name { get; set; } = string.Empty;
    internal int Level { get; set; }
    internal uint Characteristics { get; set; }
    internal DateTime TimeDateStamp { get; set; }
    internal ushort MajorVersion { get; set; }
    internal ushort MinorVersion { get; set; }
    internal ushort NumberOfNamedEntries { get; set; }
    internal ushort NumberOfIdEntries { get; set; }

    internal List<ResourceDirectory> Subdirectories { get; set; } = new List<ResourceDirectory>();
    internal List<ResourceDataEntry> DataEntries { get; set; } = new List<ResourceDataEntry>();

    public override string ToString() => $"{Name} [DataEntries: {DataEntries.Count}] [Subdirectories: {Subdirectories.Count}]";

    internal ResourceDirectory? GetDirectory(string directoryName)
    {
        if (Level != 1)
            return null;

        var foundDirectory = Subdirectories.FirstOrDefault(x => x.Name.ToLower().Contains(directoryName.ToLower()));
        if (foundDirectory is null)
            return null;

        return foundDirectory;
    }

    internal ResourceDataEntry[]? GetResources(string directoryName)
    {
        if (Level != 1)
            return null;

        var foundDirectory = Subdirectories.FirstOrDefault(x => x.Name.ToLower().Contains(directoryName.ToLower()));
        if (foundDirectory is null)
            return null;

        var dataEntries = new ResourceDataEntry[foundDirectory.Subdirectories.Count];
        for (int i = 0; i < foundDirectory.Subdirectories.Count; i++)
            dataEntries[i] = foundDirectory.Subdirectories[i].DataEntries[0];

        return dataEntries;
    }

    internal static ResourceDirectory? ReadFromStream(Stream stream, PE_Header peHeader)
    {
        if (peHeader.Optional is null || peHeader.Optional.ResourceTable is null)
            return null;

        var sectionHeades = SectionHeader.ReadFromStream(stream, peHeader);

        var rsrcSection = peHeader.Optional.ResourceTable.FindFileSectionHeader(sectionHeades);
        long resourceTableOffset = rsrcSection.GetFileOffset(peHeader.Optional.ResourceTable.VirtualAddress);

        var rootResourceDirectory = ReadResourceDirectory(stream, resourceTableOffset, rsrcSection);
        rootResourceDirectory.Name = "Root";

        return rootResourceDirectory;
    }

    private static ResourceDirectory ReadResourceDirectory(Stream stream, long virtualAddress, SectionHeader rsrcSection, int level = 1)
    {
        stream.Position = virtualAddress;

        var resourceDirectory = ReadResourceDirectoryBase(stream, virtualAddress, level);
        var resourceDirectoryEntries = ResourceDirectoryEntry.ReadFromStream(stream, resourceDirectory, virtualAddress);


        foreach (var entry in resourceDirectoryEntries)
            ProcessResourceDirectoryEntry(stream, resourceDirectory, entry, rsrcSection);


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
            TimeDateStamp = new DateTime(1970, 1, 1).AddSeconds(MemoryMarshal.Read<uint>(resourceDirectorySpan.Slice(4, 4))),
            MajorVersion = MemoryMarshal.Read<ushort>(resourceDirectorySpan.Slice(8, 2)),
            MinorVersion = MemoryMarshal.Read<ushort>(resourceDirectorySpan.Slice(10, 2)),
            NumberOfNamedEntries = MemoryMarshal.Read<ushort>(resourceDirectorySpan.Slice(12, 2)),
            NumberOfIdEntries = MemoryMarshal.Read<ushort>(resourceDirectorySpan.Slice(14, 2)),
            Level = level
        };

        return resourceDirectory;
    }

    private static void ProcessResourceDirectoryEntry(Stream stream, ResourceDirectory directory, ResourceDirectoryEntry entry, SectionHeader rsrcSection)
    {

        if (entry.SubdirectoryOffset != 0)
        {
            var newAddress = rsrcSection.PointerToRawData + entry.SubdirectoryOffset;
            var subResourceDirectory = ReadResourceDirectory(stream, newAddress, rsrcSection, directory.Level + 1);
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
            for (int i = 0; i < directory.DataEntries.Count; i++)
                directory.DataEntries[i].ID = entry.IntegerID;
        }

    }
}
