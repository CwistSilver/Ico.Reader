using System.Runtime.InteropServices;

namespace PeDecoder.Models;
public class ResourceDirectory
{
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public uint Characteristics { get; set; }
    public DateTime TimeDateStamp { get; set; }
    public ushort MajorVersion { get; set; }
    public ushort MinorVersion { get; set; }
    public ushort NumberOfNamedEntries { get; set; }
    public ushort NumberOfIdEntries { get; set; }

    public List<ResourceDirectory> Subdirectories { get; set; } = new List<ResourceDirectory>();
    public List<ResourceDataEntry> DataEntries { get; set; } = new List<ResourceDataEntry>();

    public override string ToString() => $"{Name} [DataEntries: {DataEntries.Count}] [Subdirectories: {Subdirectories.Count}]";

    public ResourceDirectory? GetDirectory(string directoryName)
    {
        if (Level != 1)
            return null;

        var foundDirectory = Subdirectories.FirstOrDefault(x => x.Name.ToLower().Contains(directoryName.ToLower()));
        if (foundDirectory is null)
            return null;

        return foundDirectory;
    }

    public ResourceDataEntry[]? GetResources(string directoryName)
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

    public static ResourceDirectory? ReadFromStream(Stream stream, PE_Header peHeader)
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
