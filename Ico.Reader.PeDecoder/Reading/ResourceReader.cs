using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

using Ico.Reader.PeDecoder.Models;
using Ico.Reader.PeDecoder.Utils;

namespace Ico.Reader.PeDecoder.Reading;

/// <summary>
/// Walks the resource tree: type, then name, then language, with the icon and cursor payloads at the
/// leaves.
/// </summary>
internal static class ResourceReader
{
    /// <summary>The tree level holding one directory per resource type.</summary>
    private const int ResourceTypeLevel = 2;

    public static ResourceDirectory? Read(Stream stream, PeHeader peHeader)
    {
        var resourceTable = peHeader.Optional?.ResourceTable;
        if (resourceTable is null || resourceTable.VirtualAddress == 0)
            return null;

        var sectionHeaders = SectionHeaderReader.Read(stream, peHeader);

        var rsrcSection = resourceTable.FindFileSectionHeader(sectionHeaders);
        long rootOffset = rsrcSection.GetFileOffset(resourceTable.VirtualAddress);

        var rootResourceDirectory = ReadResourceDirectory(stream, rootOffset, rootOffset, [], 1);

        return rootResourceDirectory with { Name = ResourceDirectory.RootName, Sections = sectionHeaders };
    }

    /// <summary>
    /// Reads the directory at <paramref name="directoryOffset"/> and everything below it. Every offset stored in the
    /// tree counts from its root, which is the start of the section only when the tree comes first in it.
    /// </summary>
    private static ResourceDirectory ReadResourceDirectory(Stream stream, long rootOffset, long directoryOffset, HashSet<long> visited, int level)
    {
        stream.Position = directoryOffset;

        var resourceDirectory = ReadResourceDirectoryBase(stream, level);
        if (level >= ResourceDirectory.MaxDepth || !visited.Add(directoryOffset))
            return resourceDirectory;

        var subdirectories = new List<ResourceDirectory>();
        var dataEntries = new List<ResourceDataEntry>();

        foreach (var entry in ReadDirectoryEntries(stream, resourceDirectory, directoryOffset))
        {
            if (entry.SubdirectoryOffset == 0)
            {
                dataEntries.Add(ReadDataEntry(stream, rootOffset + entry.DataEntryOffset));
                continue;
            }

            var child = ReadResourceDirectory(stream, rootOffset, rootOffset + entry.SubdirectoryOffset, visited, level + 1);

            subdirectories.Add(NameFromParentEntry(child, entry, stream, rootOffset));
        }

        return resourceDirectory with { Subdirectories = subdirectories, DataEntries = dataEntries };
    }

    private static ResourceDirectory ReadResourceDirectoryBase(Stream stream, int level)
    {
        Span<byte> resourceDirectoryBytes = stackalloc byte[16];
        stream.ReadExactly(resourceDirectoryBytes);

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

    /// <summary>
    /// A directory is named by the entry in its parent that points at it: an explicit name, a
    /// resource type at the second level, or an id below that. At the id level that id also names
    /// the resource itself, so it is stamped onto the data entries.
    /// </summary>
    private static ResourceDirectory NameFromParentEntry(ResourceDirectory directory, ResourceDirectoryEntry entry, Stream stream, long rootOffset)
    {
        if (entry.NameOffset != 0)
            return directory with { Name = DecodeName(entry, stream, rootOffset) };

        if (directory.Level == ResourceTypeLevel)
            return directory with { Name = ((ResourceType)entry.IntegerID).ToString() };

        return directory with
        {
            Name = entry.IntegerID.ToString(CultureInfo.InvariantCulture),
            DataEntries = [.. directory.DataEntries.Select(x => x with { ID = entry.IntegerID })]
        };
    }

    private static string DecodeName(ResourceDirectoryEntry entry, Stream resourceStream, long rootOffset)
    {
        resourceStream.Position = rootOffset + entry.NameOffset;

        Span<byte> lengthBytes = stackalloc byte[2];
        resourceStream.ReadExactly(lengthBytes);
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

    private static ResourceDataEntry ReadDataEntry(Stream stream, long dataEntryOffset)
    {
        stream.Position = dataEntryOffset;

        Span<byte> data = stackalloc byte[16];
        stream.ReadExactly(data);

        ReadOnlySpan<byte> readOnlyData = data;

        return new ResourceDataEntry
        {
            DataRVA = MemoryMarshal.Read<uint>(readOnlyData.Slice(0, 4)),
            Size = MemoryMarshal.Read<uint>(readOnlyData.Slice(4, 4)),
            Codepage = MemoryMarshal.Read<uint>(readOnlyData.Slice(8, 4)),
            Reserved = MemoryMarshal.Read<uint>(readOnlyData.Slice(12, 4))
        };
    }
}
