using System.Runtime.InteropServices;
using System.Text;

namespace Ico.Reader.Data.Exe;
internal class ResourceDirectoryEntry
{
    internal const byte ResourceDirectoryEntrySize = 8;

    internal uint NameOffset { get; set; }
    internal uint IntegerID { get; set; }
    internal uint DataEntryOffset { get; set; }
    internal uint SubdirectoryOffset { get; set; }

    internal string DecodeName(Stream resourceStream, long streamOffset)
    {
        resourceStream.Position = streamOffset + NameOffset;

        Span<byte> lengthBytes = stackalloc byte[2];
        resourceStream.Read(lengthBytes);
        ushort nameLength = MemoryMarshal.Read<ushort>(lengthBytes);

        Span<byte> nameBytes = stackalloc byte[nameLength * 2];
        resourceStream.Read(nameBytes);

        return Encoding.Unicode.GetString(nameBytes);
    }

    internal static ResourceDirectoryEntry[] ReadFromStream(Stream stream, ResourceDirectory resourceDirectory, long resourceDirectoryOffset)
    {
        stream.Position = resourceDirectoryOffset + 16;
        var total = resourceDirectory.NumberOfNamedEntries + resourceDirectory.NumberOfIdEntries;
        var entries = new ResourceDirectoryEntry[total];

        Span<byte> data = stackalloc byte[total * ResourceDirectoryEntrySize];
        stream.Read(data);

        ReadOnlySpan<byte> readOnlyData = data;

        for (int i = 0; i < total; i++)
        {
            var entrySpan = readOnlyData.Slice(ResourceDirectoryEntrySize * i, ResourceDirectoryEntrySize);
            entries[i] = new ResourceDirectoryEntry();
            AddNameOrId(entries[i], entrySpan);
            AddSubdirectoryOrDataEntry(entries[i], entrySpan);
        }

        return entries;
    }

    private static void AddNameOrId(ResourceDirectoryEntry entry, ReadOnlySpan<byte> entrySpan)
    {
        var nameOffsetOrIntegerID = MemoryMarshal.Read<uint>(entrySpan.Slice(0, 4));
        if ((nameOffsetOrIntegerID & 0x80000000) != 0)
            entry.NameOffset = nameOffsetOrIntegerID & 0x7FFFFFFF;
        else
            entry.IntegerID = nameOffsetOrIntegerID;
    }

    private static void AddSubdirectoryOrDataEntry(ResourceDirectoryEntry entry, ReadOnlySpan<byte> entrySpan)
    {
        var offset = MemoryMarshal.Read<uint>(entrySpan.Slice(4, 4));
        bool isSubdirectoryOffset = (offset & 0x80000000) != 0;
        if (isSubdirectoryOffset)
            entry.SubdirectoryOffset = offset & 0x7FFFFFFF;
        else
            entry.DataEntryOffset = offset;
    }
}
