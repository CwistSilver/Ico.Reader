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
        var data = new byte[(int)imageReference.Size];
        stream.Position = imageReference.Offset;
        stream.Read(data, 0, data.Length);

        return icoDecoder.GetImageData(data, imageReference.Format);
    }

    public static async Task<byte[]> ReadAsync(Stream stream, ImageReference imageReference, IIcoDecoder icoDecoder, CancellationToken cancellationToken)
    {
        var data = new byte[(int)imageReference.Size];
        stream.Position = imageReference.Offset;
        await stream.ReadAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);

        return icoDecoder.GetImageData(data, imageReference.Format);
    }
}
