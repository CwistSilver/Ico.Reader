using Ico.Reader.Data;
using Ico.Reader.Decoder;

namespace Ico.Reader.Reading;

/// <summary>
/// Reads the bytes an <see cref="ImageReference"/> points at and decodes them to PNG.
/// </summary>
internal static class ImageDataReader
{
    public static byte[] Read(Stream stream, ImageReference imageReference, IIcoDecoder icoDecoder)
    {
        var data = new byte[CheckedSize(stream, imageReference)];
        stream.Position = imageReference.Offset;
        stream.ReadExactly(data, 0, data.Length);

        return icoDecoder.GetImageData(data, imageReference.Format);
    }

    public static async Task<byte[]> ReadAsync(Stream stream, ImageReference imageReference, IIcoDecoder icoDecoder, CancellationToken cancellationToken)
    {
        var data = new byte[CheckedSize(stream, imageReference)];
        stream.Position = imageReference.Offset;
        await stream.ReadExactlyAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);

        return icoDecoder.GetImageData(data, imageReference.Format);
    }

    /// <summary>
    /// The size comes from the file, so it is checked against the stream before a buffer that large is allocated.
    /// </summary>
    /// <exception cref="EndOfStreamException">The image runs past the end of the stream.</exception>
    private static int CheckedSize(Stream stream, ImageReference imageReference)
    {
        if ((long)imageReference.Offset + imageReference.Size > stream.Length)
        {
            throw new EndOfStreamException(
                $"The image at offset {imageReference.Offset} declares {imageReference.Size} bytes, but the data ends after {stream.Length}.");
        }

        return (int)imageReference.Size;
    }
}
