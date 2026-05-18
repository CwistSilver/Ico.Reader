using System.Runtime.InteropServices;

using Ico.Reader.Data;

namespace Ico.Reader.Utils;

public static class IcoDirectoryEntryUtils
{
    /// <summary>
    /// Reads ico directory entries from a stream based on the specified ico header. This method is typically used when reading icos from ICO files.
    /// <para>
    /// For more information, see <see href="https://en.wikipedia.org/wiki/ICO_(file_format)#Structure_of_image_directory">Structure of image directory</see>.
    /// </para>
    /// </summary>
    /// <param name="stream">The stream from which to read the ico directory entries.</param>
    /// <param name="icoHeader">The header that provides information about the number of images.</param>
    /// <returns>An array of <see cref="IconDirectoryEntry"/> objects representing the ico directory entries read from the stream.</returns>
    public static IIcoDirectoryEntry[] ReadEntriesFromStream(Stream stream, IcoHeader icoHeader)
    {
        var positionStart = stream.Position;
        var byteSize = 16 * icoHeader.ImageCount;
        var entries = new IIcoDirectoryEntry[icoHeader.ImageCount];

        Span<byte> entriesBuffer = stackalloc byte[byteSize];
        stream.Read(entriesBuffer);
        ReadOnlySpan<byte> entriesBufferSpan = entriesBuffer;

        for (var i = 0; i < icoHeader.ImageCount; i++)
        {
            var offset = i * 16;

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
                    ImageOffset = MemoryMarshal.Read<uint>(entriesBufferSpan.Slice(offset + 12, 4))
                };
            }
            else if (icoHeader.ImageType == 2)
            {
                entries[i] = new CursorDirectoryEntry
                {
                    Width = entriesBufferSpan[offset],
                    Height = entriesBufferSpan[offset + 1],
                    Planes = 0,
                    HotspotX = MemoryMarshal.Read<ushort>(entriesBufferSpan.Slice(offset + 4, 2)),
                    HotspotY = MemoryMarshal.Read<ushort>(entriesBufferSpan.Slice(offset + 6, 2)),
                    ImageSize = MemoryMarshal.Read<uint>(entriesBufferSpan.Slice(offset + 8, 4)),
                    ImageOffset = MemoryMarshal.Read<uint>(entriesBufferSpan.Slice(offset + 12, 4))
                };
            }

            entries[i].RealImageOffset = entries[i].ImageOffset;
        }

        return entries;
    }

    public static T[] ReadEntriesFromEXEStream<T>(Stream stream, IcoHeader icoHeader) where T : class, IIcoDirectoryEntry
    {
        if (icoHeader.ImageType == IconDirectoryEntry.ImageType && typeof(T) != typeof(IconDirectoryEntry))
            throw new Exception("Invalid entry type");
        else if (icoHeader.ImageType == CursorDirectoryEntry.ImageType && typeof(T) != typeof(CursorDirectoryEntry))
            throw new Exception("Invalid entry type");

        var entries = ReadEntriesFromStream(stream, icoHeader);
        return (T[])entries;
    }
}
