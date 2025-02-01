using System.Runtime.InteropServices;

namespace Ico.Reader.Data;

public interface IIcoGroup : IIcoGroup<IIcoDirectoryEntry>
{

    /// <summary>
    /// Reads ico directory entries from an executable file stream based on the specified ico header. This method adapts the process for the differences in ico data layout within EXE or DLL files.
    /// <para>
    /// For more information, see <see href="https://www.codeguru.com/windows/hacking-ico-resources/">Hacking Ico Resources</see>.
    /// </para>
    /// </summary>
    /// <param name="stream">The stream from which to read the ico directory entries, typically an EXE or DLL file stream.</param>
    /// <param name="icoHeader">The header that provides information about the number of images and their properties.</param>
    /// <returns>An array of <see cref="IconDirectoryEntry"/> objects representing the ico directory entries read from the executable file stream.</returns>
    public static IIcoDirectoryEntry[] ReadFromEXEStream(Stream stream, IcoHeader icoHeader)
    {
        var positionStart = stream.Position;

        int byteSize = 14 * icoHeader.ImageCount;
        var entries = new IIcoDirectoryEntry[icoHeader.ImageCount];

        Span<byte> entriesBuffer = stackalloc byte[byteSize];
        stream.Read(entriesBuffer);
        ReadOnlySpan<byte> entriesBufferSpan = entriesBuffer;

        for (int i = 0; i < icoHeader.ImageCount; i++)
        {
            var offset = i * 14;
            ushort resourceID = MemoryMarshal.Read<ushort>(entriesBufferSpan.Slice(offset + 12, 2));

            if (icoHeader.ImageType == 1)
            {
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
            else if (icoHeader.ImageType == 2)
            {
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
        }

        return entries;
    }
}

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

