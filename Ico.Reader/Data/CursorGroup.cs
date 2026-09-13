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

    private readonly IReadOnlyList<CursorDirectoryEntry> _directoryEntries = [];

    /// <summary>
    /// The <see cref="CursorDirectoryEntry"/> of each cursor within the group. The group keeps its own copy of the
    /// entries it is created with.
    /// </summary>
    public required IReadOnlyList<CursorDirectoryEntry> DirectoryEntries
    {
        get => _directoryEntries;
        init => _directoryEntries = Array.AsReadOnly(value.ToArray());
    }

    /// <summary> <inheritdoc/> </summary>
    public int Size => DirectoryEntries.Count;

    IReadOnlyList<IIcoDirectoryEntry> IIcoGroup<IIcoDirectoryEntry>.DirectoryEntries => DirectoryEntries;

    /// <inheritdoc/>
    public override string ToString() => $"[{nameof(CursorGroup)}] {Name} ({Size})";
}
