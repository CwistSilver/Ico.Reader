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

    /// <summary>
    /// An array of <see cref="IconDirectoryEntry"/> objects, each representing an icon within the group.
    /// This array contains metadata about individual icons.
    /// </summary>
    public required IconDirectoryEntry[] DirectoryEntries { get; init; }

    /// <summary> <inheritdoc/> </summary>
    public int Size => DirectoryEntries.Length;

    IIcoDirectoryEntry[] IIcoGroup<IIcoDirectoryEntry>.DirectoryEntries => DirectoryEntries;

    public override string ToString() => $"[{nameof(IconGroup)}] {Name} ({Size})";
}
