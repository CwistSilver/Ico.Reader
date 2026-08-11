using System.Runtime.InteropServices;
using System.Text;

using PeDecoder.Models;
using PeDecoder.Utils;

namespace PeDecoder.Reading;

/// <summary>
/// Walks the resource tree: type, then name, then language, with the icon and cursor payloads at the
/// leaves.
/// </summary>
internal static class ResourceReader
{
    public static ResourceDirectory? Read(Stream stream, PeHeader peHeader)
    {
        if (peHeader.Optional is null || peHeader.Optional.ResourceTable is null)
            return null;

        var sectionHeaders = SectionHeaderReader.Read(stream, peHeader);

        var rsrcSection = peHeader.Optional.ResourceTable.FindFileSectionHeader(sectionHeaders);
        long resourceTableOffset = rsrcSection.GetFileOffset(peHeader.Optional.ResourceTable.VirtualAddress);

        var rootResourceDirectory = ReadResourceDirectory(stream, resourceTableOffset, rsrcSection, [], 1);
        rootResourceDirectory.Name = ResourceDirectory.RootName;
        rootResourceDirectory.Section = rsrcSection;

        return rootResourceDirectory;
    }

    private static ResourceDirectory ReadResourceDirectory(Stream stream, long virtualAddress, SectionHeader rsrcSection, HashSet<long> visited, int level)
    {
        stream.Position = virtualAddress;

        var resourceDirectory = ReadResourceDirectoryBase(stream, virtualAddress, level);
        if (level >= ResourceDirectory.MaxDepth || !visited.Add(virtualAddress))
            return resourceDirectory;

        var resourceDirectoryEntries = ReadDirectoryEntries(stream, resourceDirectory, virtualAddress);

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
            var dataEntry = ReadDataEntry(stream, rsrcSection.PointerToRawData, entry.DataEntryOffset);
            directory.DataEntries.Add(dataEntry);
        }
    }

    private static void SetName(Stream stream, ResourceDirectory directory, ResourceDirectoryEntry entry, SectionHeader rsrcSection)
    {
        if (entry.NameOffset != 0)
        {
            directory.Name = DecodeName(entry, stream, rsrcSection.PointerToRawData);
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

    private static string DecodeName(ResourceDirectoryEntry entry, Stream resourceStream, long streamOffset)
    {
        resourceStream.Position = streamOffset + entry.NameOffset;

        Span<byte> lengthBytes = stackalloc byte[2];
        resourceStream.Read(lengthBytes);
        var nameLength = MemoryMarshal.Read<ushort>(lengthBytes);

        // A name is UTF-16 and its length is a ushort, so this can ask for 128 KB of stack.
        return PooledStreamReader.Read(resourceStream, nameLength * 2, Encoding.Unicode.GetString);
    }

    private static ResourceDirectoryEntry[] ReadDirectoryEntries(Stream stream, ResourceDirectory resourceDirectory, long resourceDirectoryOffset)
    {
        stream.Position = resourceDirectoryOffset + ResourceDirectory.HeaderSize;
        var total = resourceDirectory.NumberOfNamedEntries + resourceDirectory.NumberOfIdEntries;

        return PooledStreamReader.Read(stream, total * ResourceDirectoryEntry.ResourceDirectoryEntrySize, data =>
        {
            var entries = new ResourceDirectoryEntry[total];

            for (var i = 0; i < total; i++)
            {
                    entries[i] = ParseDirectoryEntry(data.Slice(ResourceDirectoryEntry.ResourceDirectoryEntrySize * i, ResourceDirectoryEntry.ResourceDirectoryEntrySize));
            }

            return entries;
        });
    }

    /// <summary>
    /// Parses one directory entry. The high bit of each field selects which of two meanings it
    /// carries: a name offset or an integer id, and a subdirectory offset or a data entry offset.
    /// </summary>
    private static ResourceDirectoryEntry ParseDirectoryEntry(ReadOnlySpan<byte> entrySpan)
    {
        const uint HighBit = 0x80000000;
        const uint OffsetMask = 0x7FFFFFFF;

        var nameOrId = MemoryMarshal.Read<uint>(entrySpan.Slice(0, 4));
        var offset = MemoryMarshal.Read<uint>(entrySpan.Slice(4, 4));

        var isName = (nameOrId & HighBit) != 0;
        var isSubdirectory = (offset & HighBit) != 0;

        return new ResourceDirectoryEntry
        {
            NameOffset = isName ? nameOrId & OffsetMask : 0,
            IntegerID = isName ? 0 : nameOrId,
            SubdirectoryOffset = isSubdirectory ? offset & OffsetMask : 0,
            DataEntryOffset = isSubdirectory ? 0 : offset
        };
    }

    private static ResourceDataEntry ReadDataEntry(Stream stream, long baseOffset, uint dataEntryOffset)
    {
        stream.Position = baseOffset + dataEntryOffset;

        Span<byte> data = stackalloc byte[16];
        stream.Read(data);

        ReadOnlySpan<byte> readOnlyData = data;

        var dataEntry = new ResourceDataEntry
        {
            DataRVA = MemoryMarshal.Read<uint>(readOnlyData.Slice(0, 4)),
            Size = MemoryMarshal.Read<uint>(readOnlyData.Slice(4, 4)),
            Codepage = MemoryMarshal.Read<uint>(readOnlyData.Slice(8, 4)),
            Reserved = MemoryMarshal.Read<uint>(readOnlyData.Slice(12, 4))
        };

        if (dataEntry.Reserved != 0)
            throw new InvalidDataException($"{nameof(ResourceDataEntry)}: Reserved must be 0 but was {dataEntry.Reserved}.");

        return dataEntry;
    }
}
