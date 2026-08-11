namespace PeDecoder.Models;

internal sealed class ResourceDirectoryEntry
{
    public const byte ResourceDirectoryEntrySize = 8;

    public uint NameOffset { get; set; }
    public uint IntegerID { get; set; }
    public uint DataEntryOffset { get; set; }
    public uint SubdirectoryOffset { get; set; }
}
