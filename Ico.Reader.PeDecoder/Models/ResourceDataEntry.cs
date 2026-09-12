namespace Ico.Reader.PeDecoder.Models;

/// <summary>
/// A leaf of the resource tree, pointing at the bytes of one resource.
/// </summary>
public sealed record ResourceDataEntry
{
    /// <summary>
    /// Identifier of the language subdirectory this entry was found under.
    /// </summary>
    public uint ID { get; init; }

    /// <summary>
    /// Address of the resource bytes relative to the image base once loaded. Use
    /// <see cref="GetFileOffset"/> to turn it into a file offset.
    /// </summary>
    public uint DataRVA { get; init; }

    /// <summary>
    /// Size of the resource in bytes.
    /// </summary>
    public uint Size { get; init; }

    /// <summary>
    /// Code page used to decode the resource when it holds text.
    /// </summary>
    public uint Codepage { get; init; }

    /// <summary>
    /// Reserved and required to be zero.
    /// </summary>
    public uint Reserved { get; init; }

    /// <summary>
    /// Resolves this entry's virtual address to a file offset within the resource section.
    /// </summary>
    /// <param name="resourceSection">
    /// The section the resource tree was read from, available as <see cref="ResourceDirectory.Section"/>.
    /// </param>
    /// <returns>The offset of the resource bytes within the file.</returns>
    public uint GetFileOffset(SectionHeader resourceSection) => resourceSection.GetFileOffset(DataRVA);

    /// <inheritdoc />
    public override string ToString() => $"ID: {ID}, DataRVA: {DataRVA}, Size: {Size}, Codepage: {Codepage}, Reserved: {Reserved}";
}
