namespace Ico.Reader.PeDecoder.Models;

/// <summary>
/// A leaf of the resource tree, pointing at the bytes of one resource.
/// </summary>
public sealed record ResourceDataEntry
{
    /// <summary>
    /// Identifier of the resource this entry holds, such as the ID of a cursor. Zero when the resource is
    /// identified by name, which is then the <see cref="ResourceDirectory.Name"/> of the directory holding
    /// this entry.
    /// </summary>
    public uint ID { get; init; }

    /// <summary>
    /// Address of the resource bytes relative to the image base once loaded. Use
    /// <see cref="TryGetFileOffset"/> to turn it into a file offset.
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
    /// Resolves this entry's virtual address to the file offset of its bytes.
    /// </summary>
    /// <remarks>
    /// Resolving fails when no section stores every byte of the resource in the file. A packer can leave data entries
    /// pointing at memory it only fills at run time, as UPX does for the icons it compresses.
    /// </remarks>
    /// <param name="sections">The section table of the image, available as <see cref="ResourceDirectory.Sections"/>.</param>
    /// <param name="fileOffset">The offset of the resource bytes within the file, when resolving succeeds.</param>
    /// <returns><see langword="true"/> if the file holds the resource bytes.</returns>
    public bool TryGetFileOffset(IReadOnlyList<SectionHeader> sections, out uint fileOffset)
    {
        foreach (var section in sections)
        {
            if (section.TryGetFileOffset(DataRVA, Size, out fileOffset))
                return true;
        }

        fileOffset = 0;
        return false;
    }

    /// <inheritdoc />
    public override string ToString() => $"ID: {ID}, DataRVA: {DataRVA}, Size: {Size}, Codepage: {Codepage}, Reserved: {Reserved}";
}
