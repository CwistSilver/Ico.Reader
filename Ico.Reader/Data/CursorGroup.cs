namespace Ico.Reader.Data;

/// <summary>
/// Represents a collection of <see cref="CursorDirectoryEntry"/> within an CUR file.
/// </summary>
public sealed class CursorGroup : IIcoGroup<CursorDirectoryEntry>, IIcoGroup
{
    /// <summary> <inheritdoc/> </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The type of ico group.
    /// <para>
    /// for <see cref="CursorGroup"/> will always be <see cref="IcoType.Cursor"/>
    /// </para>
    /// </summary>
    public IcoType IcoType => IcoType.Cursor;

    /// <summary> <inheritdoc/> </summary>
    public required IcoHeader Header { get; init; }

    /// <summary>
    /// An array of <see cref="CursorDirectoryEntry"/> objects, each representing a cursor within the group.
    /// This array contains metadata about individual cursors.
    /// </summary>
    public required CursorDirectoryEntry[] DirectoryEntries { get; init; }

    /// <summary> <inheritdoc/> </summary>
    public int Size => DirectoryEntries.Length;

    IIcoDirectoryEntry[] IIcoGroup<IIcoDirectoryEntry>.DirectoryEntries => DirectoryEntries;

    /// <inheritdoc/>
    public override string ToString() => $"[{nameof(CursorGroup)}] {Name} ({Size})";
}
