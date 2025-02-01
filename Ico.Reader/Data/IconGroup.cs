using System.Runtime.InteropServices;

namespace Ico.Reader.Data;
/// <summary>
/// Represents a collection of <see cref="IconDirectoryEntry"/> within an ICO file.
/// </summary>
public sealed class IconGroup : IIcoGroup<IconDirectoryEntry>, IIcoGroup
{
    /// <summary> <inheritdoc/> </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Specifies the type of the ICO group.
    /// <para>
    /// For <see cref="IconGroup"/>, this will always be <see cref="IcoType.Icon"/>.
    /// </para>
    /// </summary>
    public IcoType IcoType => IcoType.Icon;

    /// <summary> <inheritdoc/> </summary>
    public IcoHeader Header { get; set; } = null!;

    /// <summary>
    /// An array of <see cref="IconDirectoryEntry"/> objects, each representing an icon within the group.
    /// This array contains metadata about individual icons.
    /// </summary>
    public IconDirectoryEntry[] DirectoryEntries { get; set; } = Array.Empty<IconDirectoryEntry>();

    /// <summary> <inheritdoc/> </summary>
    public int Size => DirectoryEntries?.Length ?? 0;

    IIcoDirectoryEntry[] IIcoGroup<IIcoDirectoryEntry>.DirectoryEntries
    {
        get => DirectoryEntries;
        set
        {
            var cursorDirectoryEntries = new IconDirectoryEntry[value.Length];

            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] is IconDirectoryEntry entry)
                    cursorDirectoryEntries[i] = entry;
                else
                    throw new ArgumentException("Invalid entry type", nameof(value));
            }
            DirectoryEntries = cursorDirectoryEntries;
        }
    }

    public override string ToString() => $"[{nameof(IconGroup)}] {Name} ({Size})";

    public IconDirectoryEntry[] ReadEntriesFromEXEStream(Stream stream, IcoHeader icoHeader)
    {
        if (icoHeader.ImageType != IconDirectoryEntry.ImageType)
        {
            throw new Exception("The ico data does not contain icon data.");
        }

        var positionStart = stream.Position;

        int byteSize = 14 * icoHeader.ImageCount;
        var entries = new IconDirectoryEntry[icoHeader.ImageCount];

        Span<byte> entriesBuffer = stackalloc byte[byteSize];
        stream.Read(entriesBuffer);
        ReadOnlySpan<byte> entriesBufferSpan = entriesBuffer;

        for (int i = 0; i < icoHeader.ImageCount; i++)
        {
            var offset = i * 14;
            entries[i] = new IconDirectoryEntry
            {
                Width = entriesBufferSpan[offset],
                Height = entriesBufferSpan[offset + 1],
                ColorCount = entriesBufferSpan[offset + 2],
                Reserved = entriesBufferSpan[offset + 3],
                Planes = MemoryMarshal.Read<ushort>(entriesBufferSpan.Slice(offset + 4, 2)),
                ColorDepth = MemoryMarshal.Read<ushort>(entriesBufferSpan.Slice(offset + 6, 2)),
                ImageSize = MemoryMarshal.Read<uint>(entriesBufferSpan.Slice(offset + 8, 4)),
                ImageOffset = MemoryMarshal.Read<ushort>(entriesBufferSpan.Slice(offset + 12, 2))
            };
        }

        return entries;
    }

    IIcoDirectoryEntry[] IIcoGroup<IIcoDirectoryEntry>.ReadEntriesFromEXEStream(Stream stream, IcoHeader icoHeader) => IIcoGroup.ReadFromEXEStream(stream, icoHeader);
}