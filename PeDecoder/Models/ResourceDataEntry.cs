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

    public override string ToString() => $"ID: {ID}, DataRVA: {DataRVA}, Size: {Size}, Codepage: {Codepage}, Reserved: {Reserved}";
}
