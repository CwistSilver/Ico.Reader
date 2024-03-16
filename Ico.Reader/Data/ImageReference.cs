using Ico.Reader.Decoder;

namespace Ico.Reader.Data;
/// <summary>
/// Contains metadata about an individual image within an ico file.
/// </summary>
public sealed class ImageReference
{
    /// <summary>
    /// The Id of the image within the PE file.
    /// </summary>
    public int Id { get; internal set; }

    /// <summary>
    /// The offset in the file where the image data begins.
    /// </summary>
    public uint Offset { get; internal set; }

    /// <summary>
    /// The size of the image data in bytes.
    /// </summary>
    public uint Size { get; internal set; }

    /// <summary>
    /// The width of the image in pixels.
    /// </summary>
    public int Width { get; internal set; }

    /// <summary>
    /// The height of the image in pixels.
    /// </summary>
    public int Height { get; internal set; }

    /// <summary>
    /// The bit depth of the image, indicating the number of bits used for each color component.
    /// </summary>
    public int BitCount { get; internal set; }

    /// <summary>
    /// The format of the image, specifying how the image data is encoded.
    /// </summary>
    public IcoImageFormat Format { get; internal set; }

    internal ImageReference() { }

    /// <summary>
    /// Extracts the image data for this reference from a stream using the specified ico decoder.
    /// </summary>
    /// <param name="stream">The stream containing the ico data.</param>
    /// <param name="icoDecoder">The decoder to use for extracting the image data.</param>
    /// <returns>A byte array containing the image data.</returns>
    public byte[] GetImageData(Stream stream, IIcoDecoder icoDecoder)
    {
        stream.Position = Offset;
        Span<byte> data = new byte[(int)Size];
        stream.Read(data);

        return icoDecoder.GetImageData(data, Format);
    }

    /// <summary>
    /// Creates an ImageReference from an IcoDirectoryEntry, determining the format using an ico decoder.
    /// </summary>
    /// <param name="stream">The stream containing the ico data.</param>
    /// <param name="icoDirectoryEntry">The directory entry representing the image within the stream.</param>
    /// <param name="icoDecoder">The decoder to use for reading the image format.</param>
    /// <returns>An ImageReference if the format is recognized; otherwise, null.</returns>
    public static ImageReference? FromIcoDirectoryEntry(Stream stream, IcoDirectoryEntry icoDirectoryEntry, IIcoDecoder icoDecoder)
    {
        var imageReference = new ImageReference
        {
            Offset = icoDirectoryEntry.ImageOffset,
            Size = icoDirectoryEntry.ImageSize,
            Width = icoDirectoryEntry.Width,
            Height = icoDirectoryEntry.Height,
            BitCount = icoDirectoryEntry.ColorDepth
        };

        stream.Position = icoDirectoryEntry.ImageOffset;
        Span<byte> data = stackalloc byte[26];
        stream.Read(data);

        ReadOnlySpan<byte> readOnlyData = data;
        imageReference.Format = icoDecoder.ReadFormat(readOnlyData);

        if (imageReference.Width == 0 || imageReference.Height == 0 || imageReference.BitCount == 0)
        {
            var imageMetadata = icoDecoder.ReadImageMetadata(readOnlyData);
            if (imageMetadata is null)
                return imageReference;

            imageReference.Width = imageMetadata.Width;
            imageReference.Height = imageMetadata.Height;
            imageReference.BitCount = imageMetadata.BitCount;
        }

        return imageReference;
    }

    /// <summary>
    /// Creates an ImageReference directly from a stream at a specified offset, using an ico decoder to read metadata.
    /// </summary>
    /// <param name="stream">The stream containing the ico data.</param>
    /// <param name="offset">The offset in the stream where the image data begins.</param>
    /// <param name="size">The size of the image data in bytes.</param>
    /// <param name="icoDecoder">The decoder to use for reading the image metadata.</param>
    /// <returns>An ImageReference if the metadata can be read; otherwise, null.</returns>
    public static ImageReference? FromStream(Stream stream, uint offset, uint size, IIcoDecoder icoDecoder)
    {
        stream.Position = offset;
        Span<byte> data = stackalloc byte[26];
        stream.Read(data);
        var imageHeaderSpan = data;

        var imageReference = icoDecoder.ReadImageMetadata(imageHeaderSpan);
        if (imageReference is null)
            return null;

        imageReference.Offset = offset;
        imageReference.Size = size;

        return imageReference;
    }
}
