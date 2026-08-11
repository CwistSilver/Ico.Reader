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

        // A directory entry stores width and height in a single byte each and uses 0 for 256, so
        // anything it cannot express has to come from the image header.
        var declaresItsOwnSize = directoryEntry.Width != 0 && directoryEntry.Height != 0 && directoryEntry.ColorDepth != 0;

        var imageReference = new ImageReference
        {
            Offset = directoryEntry.ImageOffset,
            Size = directoryEntry.ImageSize,
            Width = declaresItsOwnSize ? directoryEntry.Width : imageMetadata.Width,
            Height = declaresItsOwnSize ? directoryEntry.Height : imageMetadata.Height,
            BitCount = declaresItsOwnSize ? directoryEntry.ColorDepth : imageMetadata.BitCount,
            Format = imageMetadata.Format
        };

        return directoryEntry is CursorDirectoryEntry cursor
            ? imageReference.AsCursor(cursor.HotspotX, cursor.HotspotY)
            : imageReference;
    }

    /// <summary>
    /// Creates a reference for an image that has no directory entry, as with a PE resource, taking
    /// every value from the image header.
    /// </summary>
    /// <returns>A reference, or null when the image is in a format no decoder recognises.</returns>
    public static ImageReference? FromStream(Stream stream, uint offset, uint size, IIcoDecoder icoDecoder)
    {
        return ReadMetadata(stream, offset, icoDecoder)?.WithLocation(offset, size);
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
