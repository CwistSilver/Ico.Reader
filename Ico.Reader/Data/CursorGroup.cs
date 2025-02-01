using System.Runtime.InteropServices;

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
        set => throw new NotImplementedException();
    }

    /// <summary> <inheritdoc/> </summary>
    public int Size => DirectoryEntries?.Length ?? 0;

    public override string ToString() => $"[{nameof(CursorGroup)}] {Name} ({Size})";


    public CursorDirectoryEntry[] ReadEntriesFromEXEStream(Stream stream, IcoHeader icoHeader)
    {
        if (icoHeader.ImageType != CursorDirectoryEntry.ImageType)
        {
            throw new Exception("The ico data does not contain cursor data.");
        }

        var positionStart = stream.Position;

        int byteSize = 14 * icoHeader.ImageCount;
        var entries = new CursorDirectoryEntry[icoHeader.ImageCount];

        Span<byte> entriesBuffer = stackalloc byte[byteSize];
        stream.Read(entriesBuffer);
        ReadOnlySpan<byte> entriesBufferSpan = entriesBuffer;

        for (int i = 0; i < icoHeader.ImageCount; i++)
        {
            var offset = i * 14;
            ushort resourceID = MemoryMarshal.Read<ushort>(entriesBufferSpan.Slice(offset + 12, 2));

            entries[i] = new CursorDirectoryEntry()
            {
                Width = entriesBufferSpan[offset],
                Height = entriesBufferSpan[offset + 1],
                Planes = 0,
                HotspotX = 0,
                HotspotY = 0,
                ColorDepth = 0,
                RealImageOffset = 0,
                ImageSize = MemoryMarshal.Read<uint>(entriesBufferSpan.Slice(offset + 8, 4)),
                ImageOffset = resourceID
            };
        }

        return entries;
    }

    IIcoDirectoryEntry[] IIcoGroup<IIcoDirectoryEntry>.ReadEntriesFromEXEStream(Stream stream, IcoHeader icoHeader) => IIcoGroup.ReadFromEXEStream(stream, icoHeader);
}
