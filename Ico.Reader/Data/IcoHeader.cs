using System.Runtime.InteropServices;

namespace Ico.Reader.Data;
/// <summary>
/// Represents the header of an ico resource, detailing the ico's format and the number of images it contains.
/// This header is used to identify the structure of an ico file or resource in memory.
/// </summary>
public sealed class IcoHeader
{
    /// <summary>
    /// Reserved; must always be set to 0.
    /// </summary>
    public ushort Reserved { get; set; }

    /// <summary>
    /// Specifies the type of the image; 1 for ico (.ICO) images, 2 for cursor (.CUR) images.
    /// </summary>
    public ushort ImageType { get; set; }

    /// <summary>
    /// The number of images in the ico or cursor file.
    /// </summary>
    public ushort ImageCount { get; set; }

    /// <summary>
    /// Reads an <see cref="IcoHeader"/> from a given stream starting at a specified position.
    /// This static method allows for the creation of an <see cref="IcoHeader"/> object by reading the necessary bytes from a stream and interpreting them according to the ICO file format specification.
    /// </summary>
    /// <param name="stream">The input stream from which to read the ico header.</param>
    /// <param name="startPosition">The position in the stream at which to begin reading. Default is 0.</param>
    /// <returns>An instance of <see cref="IcoHeader"/> populated with data read from the stream.</returns>
    public static IcoHeader ReadFromStream(Stream stream, long startPosition = 0)
    {
        stream.Position = startPosition;
        Span<byte> icoHeaderBuffer = stackalloc byte[6];
        stream.Read(icoHeaderBuffer);

        ReadOnlySpan<byte> icoHeaderSpan = icoHeaderBuffer;

        return new IcoHeader
        {
            Reserved = MemoryMarshal.Read<ushort>(icoHeaderSpan.Slice(0, 2)),
            ImageType = MemoryMarshal.Read<ushort>(icoHeaderSpan.Slice(2, 2)),
            ImageCount = MemoryMarshal.Read<ushort>(icoHeaderSpan.Slice(4, 2)),
        };
    }
}
