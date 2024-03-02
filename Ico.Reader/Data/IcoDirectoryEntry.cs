using System.Runtime.InteropServices;

namespace Ico.Reader.Data;
/// <summary>
/// Represents an entry within an ico directory, containing metadata about an ico image.
/// <para>
/// For more information, see <see href="https://en.wikipedia.org/wiki/ICO_(file_format)#Structure_of_image_directory">Structure of image directory</see>.
/// </para>
/// </summary>
public sealed class IcoDirectoryEntry
{
    /// <summary>
    /// The width of the ico image in pixels; 0 means image width is 256 pixels.
    /// </summary>
    public byte Width { get; internal set; }

    /// <summary>
    /// The height of the ico image in pixels; 0 means image height is 256 pixels.
    /// </summary>
    public byte Height { get; internal set; }

    /// <summary>
    /// The number of colors in the ico's palette; 0 means the image does not use a palette.
    /// </summary>
    public byte ColorCount { get; internal set; }

    /// <summary>
    /// Reserved property, should always be set to 0.
    /// </summary>
    public byte Reserved { get; internal set; }

    /// <summary>
    /// The number of color planes; typically 0 or 1 for icos.
    /// </summary>
    public ushort Planes { get; internal set; }

    /// <summary>
    /// The bit depth of the ico image.
    /// </summary>
    public ushort ColorDepth { get; internal set; }

    /// <summary>
    /// The size of the ico image data in bytes.
    /// </summary>
    public uint ImageSize { get; internal set; }

    /// <summary>
    /// The offset where the ico image data begins in the file.
    /// </summary>
    public uint ImageOffset { get; internal set; }

    /// <summary>
    /// The real offset of the image data in the file.
    /// </summary>
    public uint RealImageOffset { get; internal set; }

    /// <summary>
    /// Reads ico directory entries from a stream based on the specified ico header. This method is typically used when reading icos from ICO files.
    /// <para>
    /// For more information, see <see href="https://en.wikipedia.org/wiki/ICO_(file_format)#Structure_of_image_directory">Structure of image directory</see>.
    /// </para>
    /// </summary>
    /// <param name="stream">The stream from which to read the ico directory entries.</param>
    /// <param name="icoHeader">The header that provides information about the number of images.</param>
    /// <returns>An array of <see cref="IcoDirectoryEntry"/> objects representing the ico directory entries read from the stream.</returns>
    public static IcoDirectoryEntry[] ReadEntriesFromStream(Stream stream, IcoHeader icoHeader)
    {
        var positionStart = stream.Position;
        int byteSize = 16 * icoHeader.ImageCount;
        var entries = new IcoDirectoryEntry[icoHeader.ImageCount];

        Span<byte> entriesBuffer = stackalloc byte[byteSize];
        stream.Read(entriesBuffer);
        ReadOnlySpan<byte> entriesBufferSpan = entriesBuffer;

        for (int i = 0; i < icoHeader.ImageCount; i++)
        {
            var offset = i * 16;

            entries[i] = new IcoDirectoryEntry
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

            entries[i].RealImageOffset = entries[i].ImageOffset;

        }

        return entries;
    }


    /// <summary>
    /// Reads ico directory entries from an executable file stream based on the specified ico header. This method adapts the process for the differences in ico data layout within EXE or DLL files.
    /// <para>
    /// For more information, see <see href="https://www.codeguru.com/windows/hacking-ico-resources/">Hacking Ico Resources</see>.
    /// </para>
    /// </summary>
    /// <param name="stream">The stream from which to read the ico directory entries, typically an EXE or DLL file stream.</param>
    /// <param name="icoHeader">The header that provides information about the number of images and their properties.</param>
    /// <returns>An array of <see cref="IcoDirectoryEntry"/> objects representing the ico directory entries read from the executable file stream.</returns>
    public static IcoDirectoryEntry[] ReadEntriesFromEXEStream(Stream stream, IcoHeader icoHeader)
    {
        var positionStart = stream.Position;

        int byteSize = 14 * icoHeader.ImageCount;
        var entries = new IcoDirectoryEntry[icoHeader.ImageCount];

        Span<byte> entriesBuffer = stackalloc byte[byteSize];
        stream.Read(entriesBuffer);
        ReadOnlySpan<byte> entriesBufferSpan = entriesBuffer;

        for (int i = 0; i < icoHeader.ImageCount; i++)
        {
            var offset = i * 14;

            entries[i] = new IcoDirectoryEntry
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
}
