using System.Runtime.InteropServices;

using Ico.Reader.Data;

namespace Ico.Reader.Utils;

public static class IcoGroupUtils
{
    internal const int ExeEntrySize = 14;

    /// <summary>
    /// Reads ico directory entries from an executable file stream based on the specified ico header. This method adapts the process for the differences in ico data layout within EXE or DLL files.
    /// <para>
    /// For more information, see <see href="https://www.codeguru.com/windows/hacking-ico-resources/">Hacking Ico Resources</see>.
    /// </para>
    /// </summary>
    /// <param name="stream">The stream from which to read the ico directory entries, typically an EXE or DLL file stream.</param>
    /// <param name="icoHeader">The header that provides information about the number of images and their properties.</param>
    /// <returns>An array of <see cref="IconDirectoryEntry"/> objects representing the ico directory entries read from the executable file stream.</returns>
    /// <exception cref="NotSupportedException">Thrown if the header declares an image type that is neither icon nor cursor.</exception>
    public static IIcoDirectoryEntry[] ReadFromEXEStream(Stream stream, IcoHeader icoHeader)
    {
        var imageType = icoHeader.ImageType;

        return DirectoryEntryReader.ReadEntries<IIcoDirectoryEntry>(stream, ExeEntrySize, icoHeader.ImageCount, entry => imageType switch
        {
            IconDirectoryEntry.ImageType => ParseIconEntry(entry),
            CursorDirectoryEntry.ImageType => ParseCursorEntry(entry),
            _ => throw new NotSupportedException($"The image type {imageType} is not supported.")
        });
    }

    internal static IconDirectoryEntry ParseIconEntry(ReadOnlySpan<byte> entry) => new()
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

    /// <summary>
    /// Parses one entry of an RT_GROUP_CURSOR directory.
    /// </summary>
    /// <remarks>
    /// Unlike an icon group, a cursor group stores width and height as words, and the height covers
    /// the stacked AND mask so it is twice the visible height. Narrowing the result to a byte keeps
    /// the format's convention that 0 stands for 256.
    /// </remarks>
    internal static CursorDirectoryEntry ParseCursorEntry(ReadOnlySpan<byte> entry) => new()
    {
        Width = (byte)MemoryMarshal.Read<ushort>(entry.Slice(0, 2)),
        Height = (byte)(MemoryMarshal.Read<ushort>(entry.Slice(2, 2)) / 2),
        Planes = MemoryMarshal.Read<ushort>(entry.Slice(4, 2)),
        ColorDepth = MemoryMarshal.Read<ushort>(entry.Slice(6, 2)),
        ImageSize = MemoryMarshal.Read<uint>(entry.Slice(8, 4)),
        ImageOffset = MemoryMarshal.Read<ushort>(entry.Slice(12, 2))
    };
}
