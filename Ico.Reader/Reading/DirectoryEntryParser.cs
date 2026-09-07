using System.Buffers;
using System.Runtime.InteropServices;

using Ico.Reader.Data;

namespace Ico.Reader.Reading;

/// <summary>
/// Reads ico and cursor directory entries.
/// <para>
/// The same directory has two layouts. A standalone ICO or CUR file uses 16 byte entries ending in a
/// file offset; inside a PE the entries are 14 bytes and end in a resource id, which the PE decoder
/// resolves to an offset afterwards.
/// </para>
/// <para>
/// For more information, see <see href="https://en.wikipedia.org/wiki/ICO_(file_format)#Structure_of_image_directory">Structure of image directory</see>
/// and <see href="https://www.codeguru.com/windows/hacking-ico-resources/">Hacking Ico Resources</see>.
/// </para>
/// </summary>
internal static class DirectoryEntryParser
{
    internal const int FileEntrySize = 16;
    internal const int ResourceEntrySize = 14;

    /// <summary>
    /// Reads the directory of a standalone ICO or CUR file.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown if the header declares an image type that is neither icon nor cursor.</exception>
    public static IIcoDirectoryEntry[] ReadFileEntries(Stream stream, IcoHeader icoHeader)
    {
        var imageType = icoHeader.ImageType;

        return ReadEntries<IIcoDirectoryEntry>(stream, FileEntrySize, icoHeader.ImageCount, entry =>
        {
            // In a file the directory offset is already the real one, so it is set here rather
            // than resolved afterwards as the PE layout requires.
            var imageOffset = MemoryMarshal.Read<uint>(entry.Slice(12, 4));

            return imageType switch
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
                    ImageOffset = imageOffset,
                    RealImageOffset = imageOffset
                },
                CursorDirectoryEntry.ImageType => new CursorDirectoryEntry
                {
                    Width = entry[0],
                    Height = entry[1],
                    Planes = 0,
                    HotspotX = MemoryMarshal.Read<ushort>(entry.Slice(4, 2)),
                    HotspotY = MemoryMarshal.Read<ushort>(entry.Slice(6, 2)),
                    ImageSize = MemoryMarshal.Read<uint>(entry.Slice(8, 4)),
                    ImageOffset = imageOffset,
                    RealImageOffset = imageOffset
                },
                _ => throw new NotSupportedException($"The image type {imageType} is not supported.")
            };
        });
    }

    /// <summary>
    /// Reads a group directory out of a PE resource.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown if the header declares an image type that is neither icon nor cursor.</exception>
    public static IIcoDirectoryEntry[] ReadResourceEntries(Stream stream, IcoHeader icoHeader)
    {
        var imageType = icoHeader.ImageType;

        return ReadEntries<IIcoDirectoryEntry>(stream, ResourceEntrySize, icoHeader.ImageCount, entry => imageType switch
        {
            IconDirectoryEntry.ImageType => ParseIconResourceEntry(entry),
            CursorDirectoryEntry.ImageType => ParseCursorResourceEntry(entry),
            _ => throw new NotSupportedException($"The image type {imageType} is not supported.")
        });
    }

    /// <summary>
    /// Reads a PE group directory as a concrete entry type.
    /// </summary>
    /// <typeparam name="T">
    /// <see cref="IconDirectoryEntry"/> for icon headers, <see cref="CursorDirectoryEntry"/> for cursor headers.
    /// </typeparam>
    /// <exception cref="ArgumentException">Thrown if <typeparamref name="T"/> does not match the image type declared by <paramref name="icoHeader"/>.</exception>
    public static T[] ReadResourceEntries<T>(Stream stream, IcoHeader icoHeader) where T : class, IIcoDirectoryEntry
    {
        if (icoHeader.ImageType == IconDirectoryEntry.ImageType && typeof(T) != typeof(IconDirectoryEntry))
            throw new ArgumentException($"An icon header requires {nameof(IconDirectoryEntry)} entries.", nameof(icoHeader));

        if (icoHeader.ImageType == CursorDirectoryEntry.ImageType && typeof(T) != typeof(CursorDirectoryEntry))
            throw new ArgumentException($"A cursor header requires {nameof(CursorDirectoryEntry)} entries.", nameof(icoHeader));

        var entries = ReadResourceEntries(stream, icoHeader);
        var typedEntries = new T[entries.Length];
        for (var i = 0; i < entries.Length; i++)
            typedEntries[i] = (T)entries[i];

        return typedEntries;
    }

    private static IconDirectoryEntry ParseIconResourceEntry(ReadOnlySpan<byte> entry) => new()
    {
        Width = entry[0],
        Height = entry[1],
        ColorCount = entry[2],
        Reserved = entry[3],
        Planes = MemoryMarshal.Read<ushort>(entry.Slice(4, 2)),
        ColorDepth = MemoryMarshal.Read<ushort>(entry.Slice(6, 2)),
        ImageSize = MemoryMarshal.Read<uint>(entry.Slice(8, 4)),
        ImageOffset = MemoryMarshal.Read<ushort>(entry.Slice(12, 2))
    };

    /// <remarks>
    /// Unlike an icon group, a cursor group stores width and height as words, and the height covers
    /// the stacked AND mask so it is twice the visible height. Narrowing the result to a byte keeps
    /// the format's convention that 0 stands for 256.
    /// </remarks>
    private static CursorDirectoryEntry ParseCursorResourceEntry(ReadOnlySpan<byte> entry) => new()
    {
        Width = (byte)MemoryMarshal.Read<ushort>(entry.Slice(0, 2)),
        Height = (byte)(MemoryMarshal.Read<ushort>(entry.Slice(2, 2)) / 2),
        Planes = MemoryMarshal.Read<ushort>(entry.Slice(4, 2)),
        ColorDepth = MemoryMarshal.Read<ushort>(entry.Slice(6, 2)),
        ImageSize = MemoryMarshal.Read<uint>(entry.Slice(8, 4)),
        ImageOffset = MemoryMarshal.Read<ushort>(entry.Slice(12, 2))
    };

    /// <summary>
    /// Reads a contiguous run of fixed-size directory entries from a stream and parses each one.
    /// </summary>
    /// <remarks>
    /// The entry count comes from untrusted file data, so the scratch buffer is pooled rather than
    /// stack-allocated.
    /// </remarks>
    private static T[] ReadEntries<T>(Stream stream, int entrySize, int entryCount, EntryParser<T> parseEntry)
    {
        if (entryCount <= 0)
            return [];

        var entries = new T[entryCount];
        var byteSize = entrySize * entryCount;
        var buffer = ArrayPool<byte>.Shared.Rent(byteSize);
        try
        {
            stream.ReadExactly(buffer, 0, byteSize);

            for (var i = 0; i < entryCount; i++)
                entries[i] = parseEntry(new ReadOnlySpan<byte>(buffer, i * entrySize, entrySize));

            return entries;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private delegate T EntryParser<out T>(ReadOnlySpan<byte> entry);
}
