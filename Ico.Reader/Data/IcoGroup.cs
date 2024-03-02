namespace Ico.Reader.Data;
/// <summary>
/// Represents a group of ico images, typically extracted from an executable file (EXE) or dynamic link library (DLL).
/// </summary>
public sealed class IcoGroup
{
    /// <summary>
    /// The identifier for the ico group, used to distinguish between different groups within a source.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The header information for the ico group, containing details about the ico format and the number of images.
    /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public IcoHeader Header { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <summary>
    /// An array of directory entries, each representing an ico within the group. This array details the individual icos' metadata.
    /// </summary>
    public IcoDirectoryEntry[] DirectoryEntries { get; set; } = Array.Empty<IcoDirectoryEntry>();

    /// <summary>
    /// The number of directory entries (icos) within the group. Returns 0 if there are no entries.
    /// </summary>
    public int Size => DirectoryEntries?.Length ?? 0;

    public override string ToString() => $"{Name} ({Size})";
}