using Ico.Reader.Data;
using Ico.Reader.Decoder;

namespace Ico.Reader.Reading;

/// <summary>
/// Builds <see cref="ImageReference"/> values by combining what a directory entry declares with what
/// the image header itself reports.
/// </summary>
internal static class ImageReferenceReader
{
    /// <summary>
    /// The number of leading bytes needed to identify a format and read its dimensions: enough for a
    /// PNG IHDR, which is the larger of the two.
    /// </summary>
    private const int MetadataProbeSize = 26;

    /// <summary>
    /// Creates a reference from a directory entry, taking the format from the image data it points at.
    /// </summary>
    /// <returns>A reference, or null when the image is in a format no decoder recognises.</returns>
    public static ImageReference? FromDirectoryEntry(Stream stream, IIcoDirectoryEntry directoryEntry, IIcoDecoder icoDecoder)
    {
        var imageMetadata = ReadMetadata(stream, directoryEntry.ImageOffset, icoDecoder);
        if (imageMetadata is null)
            return null;

        var imageReference = new ImageReference
        {
            Offset = directoryEntry.ImageOffset,
            Size = directoryEntry.ImageSize,
            Width = directoryEntry.Width,
            Height = directoryEntry.Height,
            BitCount = directoryEntry.ColorDepth,
            Format = imageMetadata.Format
        };

        if (directoryEntry is CursorDirectoryEntry cursorDirectoryEntry)
        {
            imageReference.IcoType = IcoType.Cursor;
            imageReference.HotspotX = cursorDirectoryEntry.HotspotX;
            imageReference.HotspotY = cursorDirectoryEntry.HotspotY;
        }

        // A directory entry stores width and height in a single byte each and uses 0 for 256, so
        // anything it cannot express has to come from the image header.
        if (imageReference.Width == 0 || imageReference.Height == 0 || imageReference.BitCount == 0)
        {
            imageReference.Width = imageMetadata.Width;
            imageReference.Height = imageMetadata.Height;
            imageReference.BitCount = imageMetadata.BitCount;
        }

        return imageReference;
    }

    /// <summary>
    /// Creates a reference for an image that has no directory entry, as with a PE resource, taking
    /// every value from the image header.
    /// </summary>
    /// <returns>A reference, or null when the image is in a format no decoder recognises.</returns>
    public static ImageReference? FromStream(Stream stream, uint offset, uint size, IIcoDecoder icoDecoder)
    {
        var imageReference = ReadMetadata(stream, offset, icoDecoder);
        if (imageReference is null)
            return null;

        imageReference.Offset = offset;
        imageReference.Size = size;

        return imageReference;
    }

    private static ImageReference? ReadMetadata(Stream stream, uint offset, IIcoDecoder icoDecoder)
    {
        stream.Position = offset;

        Span<byte> buffer = stackalloc byte[MetadataProbeSize];
        stream.Read(buffer);

        ReadOnlySpan<byte> imageHeader = buffer;

        return icoDecoder.ReadImageMetadata(imageHeader);
    }
}
