using System.Runtime.InteropServices;

using Ico.Reader.Data;

namespace Ico.Reader.Utils;

public static class IcoGroupUtils
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

        var byteSize = 14 * icoHeader.ImageCount;
        var entries = new IIcoDirectoryEntry[icoHeader.ImageCount];

        Span<byte> entriesBuffer = stackalloc byte[byteSize];
        stream.Read(entriesBuffer);
        ReadOnlySpan<byte> entriesBufferSpan = entriesBuffer;

        for (var i = 0; i < icoHeader.ImageCount; i++)
        {
            var offset = i * 14;
            var resourceID = MemoryMarshal.Read<ushort>(entriesBufferSpan.Slice(offset + 12, 2));

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
