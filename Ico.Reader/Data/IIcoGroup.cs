namespace Ico.Reader.Data;

public interface IIcoGroup : IIcoGroup<IIcoDirectoryEntry> { }

/// <summary>
/// Represents a collection of ICO images, typically extracted from an executable file (EXE) or a dynamic link library (DLL).
/// </summary>
public interface IIcoGroup<T> where T : IIcoDirectoryEntry
{
    /// <summary>
    /// The identifier for the ico group, used to distinguish between different groups within a source.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// The type of ico group, either Icon or Cursor. <see cref="IcoType.Icon" /> or <see cref="IcoType.Cursor" />
    /// </summary>
    IcoType IcoType { get; }

    /// <summary>
    /// The header information for the ico group, containing details about the ico format and the number of images.
    /// </summary>
    IcoHeader Header { get; set; }

    /// <summary>
    /// The number of directory entries (icos) within the group. Returns 0 if there are no entries.
    /// </summary>
    int Size { get; }

    /// <summary>
    /// An array of directory entries, each representing an ico or cursor within the group.
    /// This array contains metadata about individual icons or cursors.
    /// <para>
    /// Each entry is either of type <see cref="IconDirectoryEntry"/> for ICO files
    /// or <see cref="CursorDirectoryEntry"/> for CUR files.
    /// </para>
    /// </summary>
    T[] DirectoryEntries { get; set; }

    T[] ReadEntriesFromEXEStream(Stream stream, IcoHeader icoHeader);
}

