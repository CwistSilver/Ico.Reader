namespace Ico.Reader.PeDecoder.Models;

internal sealed class ResourceDirectoryEntry
{
    public const byte ResourceDirectoryEntrySize = 8;

    public uint NameOffset { get; init; }
    public uint IntegerID { get; init; }
    public uint DataEntryOffset { get; init; }
    public uint SubdirectoryOffset { get; init; }
}
