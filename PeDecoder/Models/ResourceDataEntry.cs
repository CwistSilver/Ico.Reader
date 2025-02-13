using System.Runtime.InteropServices;

namespace PeDecoder.Models;
public class ResourceDataEntry
{
    public uint ID { get; set; }
    public uint DataRVA { get; set; }
    public uint Size { get; set; }
    public uint Codepage { get; set; }
    public uint Reserved { get; set; }

    public uint GetFileOffset(Stream stream, PE_Header peHeader)
    {
        var sectionHeades = SectionHeader.ReadFromStream(stream, peHeader);
        var rsrcSection = peHeader.Optional!.ResourceTable!.FindFileSectionHeader(sectionHeades);

        return rsrcSection.GetFileOffset(DataRVA);
    }

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
            throw new Exception($"{nameof(ResourceDataEntry)}: Reserved is not 0");

        return dataEntry;
    }

    public override string ToString() => $"ID: {ID}, DataRVA: {DataRVA}, Size: {Size}, Codepage: {Codepage}, Reserved: {Reserved}";
}
