using System.Runtime.InteropServices;

namespace Ico.Reader.Data.Exe;
internal class ResourceDataEntry
{
    internal uint ID { get; set; }
    internal uint DataRVA { get; set; }
    internal uint Size { get; set; }
    internal uint Codepage { get; set; }
    internal uint Reserved { get; set; }

    internal uint GetFileOffset(Stream stream, PE_Header peHeader)
    {
        var sectionHeades = SectionHeader.ReadFromStream(stream, peHeader);
        var rsrcSection = peHeader.Optional!.ResourceTable!.FindFileSectionHeader(sectionHeades);

        return rsrcSection.GetFileOffset(DataRVA);
    }

    internal static ResourceDataEntry ReadFromStream(Stream stream, long baseOffset, uint dataEntryOffset)
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
