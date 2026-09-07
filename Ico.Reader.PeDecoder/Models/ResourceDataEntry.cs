namespace Ico.Reader.PeDecoder.Models;

internal sealed record ResourceDataEntry
{
    public uint ID { get; init; }
    public uint DataRVA { get; init; }
    public uint Size { get; init; }
    public uint Codepage { get; init; }
    public uint Reserved { get; init; }

    /// <summary>
    /// Resolves this entry's virtual address to a file offset within the resource section.
    /// </summary>
    /// <param name="resourceSection">
    /// The section the resource tree was read from, available as <see cref="ResourceDirectory.Section"/>.
    /// </param>
    public uint GetFileOffset(SectionHeader resourceSection) => resourceSection.GetFileOffset(DataRVA);

    public override string ToString() => $"ID: {ID}, DataRVA: {DataRVA}, Size: {Size}, Codepage: {Codepage}, Reserved: {Reserved}";
}
