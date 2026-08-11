using Ico.Reader.Utils;

namespace Ico.Reader.Data;

/// <summary>
/// Represents a collection of <see cref="CursorDirectoryEntry"/> within an CUR file.
/// </summary>
public sealed class CursorGroup : IIcoGroup<CursorDirectoryEntry>, IIcoGroup
{
    /// <summary> <inheritdoc/> </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The type of ico group.
    /// <para>
    /// for <see cref="CursorGroup"/> will always be <see cref="IcoType.Cursor"/>
    /// </para>
    /// </summary>
    public IcoType IcoType => IcoType.Cursor;

    /// <summary> <inheritdoc/> </summary>
    public IcoHeader Header { get; set; } = null!;

    /// <summary>
    /// An array of <see cref="CursorDirectoryEntry"/> objects, each representing a cursor within the group.
    /// This array contains metadata about individual cursors.
    /// </summary>
    public CursorDirectoryEntry[] DirectoryEntries { get; set; } = Array.Empty<CursorDirectoryEntry>();

    IIcoDirectoryEntry[] IIcoGroup<IIcoDirectoryEntry>.DirectoryEntries
    {
        get => DirectoryEntries;
        set
        {
            var cursorDirectoryEntries = new CursorDirectoryEntry[value.Length];

            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] is CursorDirectoryEntry entry)
                    cursorDirectoryEntries[i] = entry;
                else
                    throw new ArgumentException("Invalid entry type", nameof(value));
            }
            DirectoryEntries = cursorDirectoryEntries;
        }
    }

    /// <summary> <inheritdoc/> </summary>
    public int Size => DirectoryEntries?.Length ?? 0;

    public override string ToString() => $"[{nameof(CursorGroup)}] {Name} ({Size})";
}
