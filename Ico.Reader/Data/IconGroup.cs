namespace Ico.Reader.Data;

/// <summary>
/// Represents a collection of <see cref="IconDirectoryEntry"/> within an ICO file.
/// </summary>
public sealed class IconGroup : IIcoGroup<IconDirectoryEntry>, IIcoGroup
{
    /// <summary> <inheritdoc/> </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Specifies the type of the ICO group.
    /// <para>
    /// For <see cref="IconGroup"/>, this will always be <see cref="IcoType.Icon"/>.
    /// </para>
    /// </summary>
    public IcoType IcoType => IcoType.Icon;

    /// <summary> <inheritdoc/> </summary>
    public required IcoHeader Header { get; init; }

    private readonly IReadOnlyList<IconDirectoryEntry> _directoryEntries = [];

    /// <summary>
    /// The <see cref="IconDirectoryEntry"/> of each icon within the group. The group keeps its own copy of the entries
    /// it is created with.
    /// </summary>
    public required IReadOnlyList<IconDirectoryEntry> DirectoryEntries
    {
        get => _directoryEntries;
        init => _directoryEntries = Array.AsReadOnly(value.ToArray());
    }

    /// <summary> <inheritdoc/> </summary>
    public int Size => DirectoryEntries.Count;

    IReadOnlyList<IIcoDirectoryEntry> IIcoGroup<IIcoDirectoryEntry>.DirectoryEntries => DirectoryEntries;

    /// <inheritdoc/>
    public override string ToString() => $"[{nameof(IconGroup)}] {Name} ({Size})";
}
