using System.Runtime.InteropServices;

using Ico.Reader.Data;

namespace Ico.Reader.Utils;

public static class IcoDirectoryEntryUtils
{
    internal const int FileEntrySize = 16;

    /// <summary>
    /// Reads ico directory entries from a stream based on the specified ico header. This method is typically used when reading icos from ICO files.
    /// <para>
    /// For more information, see <see href="https://en.wikipedia.org/wiki/ICO_(file_format)#Structure_of_image_directory">Structure of image directory</see>.
    /// </para>
    /// </summary>
    /// <param name="stream">The stream from which to read the ico directory entries.</param>
    /// <param name="icoHeader">The header that provides information about the number of images.</param>
    /// <returns>An array of <see cref="IconDirectoryEntry"/> objects representing the ico directory entries read from the stream.</returns>
    /// <exception cref="NotSupportedException">Thrown if the header declares an image type that is neither icon nor cursor.</exception>
    public static IIcoDirectoryEntry[] ReadEntriesFromStream(Stream stream, IcoHeader icoHeader)
    {
        var imageType = icoHeader.ImageType;

        return DirectoryEntryReader.ReadEntries<IIcoDirectoryEntry>(stream, FileEntrySize, icoHeader.ImageCount, entry =>
        {
            IIcoDirectoryEntry directoryEntry = imageType switch
            {
                IconDirectoryEntry.ImageType => new IconDirectoryEntry
                {
                    Width = entry[0],
                    Height = entry[1],
                    ColorCount = entry[2],
                    Reserved = entry[3],
                    Planes = MemoryMarshal.Read<ushort>(entry.Slice(4, 2)),
                    ColorDepth = MemoryMarshal.Read<ushort>(entry.Slice(6, 2)),
                    ImageSize = MemoryMarshal.Read<uint>(entry.Slice(8, 4)),
                    ImageOffset = MemoryMarshal.Read<uint>(entry.Slice(12, 4))
                },
                CursorDirectoryEntry.ImageType => new CursorDirectoryEntry
                {
                    Width = entry[0],
                    Height = entry[1],
                    Planes = 0,
                    HotspotX = MemoryMarshal.Read<ushort>(entry.Slice(4, 2)),
                    HotspotY = MemoryMarshal.Read<ushort>(entry.Slice(6, 2)),
                    ImageSize = MemoryMarshal.Read<uint>(entry.Slice(8, 4)),
                    ImageOffset = MemoryMarshal.Read<uint>(entry.Slice(12, 4))
                },
                _ => throw new NotSupportedException($"The image type {imageType} is not supported.")
            };

            directoryEntry.RealImageOffset = directoryEntry.ImageOffset;
            return directoryEntry;
        });
    }

    /// <summary>
    /// Reads ico directory entries from an executable file stream and returns them as a concrete entry type.
    /// </summary>
    /// <typeparam name="T">
    /// <see cref="IconDirectoryEntry"/> for icon headers, <see cref="CursorDirectoryEntry"/> for cursor headers.
    /// </typeparam>
    /// <param name="stream">The stream from which to read the ico directory entries, typically an EXE or DLL file stream.</param>
    /// <param name="icoHeader">The header that provides information about the number of images.</param>
    /// <returns>An array of <typeparamref name="T"/> representing the ico directory entries read from the stream.</returns>
    /// <exception cref="ArgumentException">Thrown if <typeparamref name="T"/> does not match the image type declared by <paramref name="icoHeader"/>.</exception>
    public static T[] ReadEntriesFromEXEStream<T>(Stream stream, IcoHeader icoHeader) where T : class, IIcoDirectoryEntry
    {
        if (icoHeader.ImageType == IconDirectoryEntry.ImageType && typeof(T) != typeof(IconDirectoryEntry))
            throw new ArgumentException($"An icon header requires {nameof(IconDirectoryEntry)} entries.", nameof(icoHeader));

        if (icoHeader.ImageType == CursorDirectoryEntry.ImageType && typeof(T) != typeof(CursorDirectoryEntry))
            throw new ArgumentException($"A cursor header requires {nameof(CursorDirectoryEntry)} entries.", nameof(icoHeader));

        var entries = IcoGroupUtils.ReadFromEXEStream(stream, icoHeader);
        var typedEntries = new T[entries.Length];
        for (var i = 0; i < entries.Length; i++)
            typedEntries[i] = (T)entries[i];

        return typedEntries;
    }
}
