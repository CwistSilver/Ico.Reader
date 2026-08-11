using System.Runtime.InteropServices;

namespace PeDecoder.Models;

internal sealed class ResourceDataEntry
{
    public uint ID { get; set; }
    public uint DataRVA { get; set; }
    public uint Size { get; set; }
    public uint Codepage { get; set; }
    public uint Reserved { get; set; }

    /// <summary>
    /// Resolves this entry's virtual address to a file offset within the resource section.
    /// </summary>
    /// <param name="resourceSection">
    /// The section the resource tree was read from, available as <see cref="ResourceDirectory.Section"/>.
    /// </param>
    public uint GetFileOffset(SectionHeader resourceSection) => resourceSection.GetFileOffset(DataRVA);

    public static ResourceDataEntry ReadFromStream(Stream stream, long baseOffset, uint dataEntryOffset)
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

    public override string ToString() => $"ID: {ID}, DataRVA: {DataRVA}, Size: {Size}, Codepage: {Codepage}, Reserved: {Reserved}";
}
