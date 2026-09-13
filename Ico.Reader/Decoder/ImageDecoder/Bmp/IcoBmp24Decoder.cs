using Ico.Reader.Data;

namespace Ico.Reader.Decoder.ImageDecoder.Bmp;

/// <summary>
/// Decodes 24 bit per pixel BMP icon data, taking transparency from the AND mask because the
/// pixels themselves carry no alpha channel.
/// </summary>
public sealed class IcoBmp24Decoder : IIcoBmpDecoder
{
    private const int BytesPerPixel = 3;

    /// <inheritdoc/>
    public byte BitCountSupported => 24;

    /// <inheritdoc/>
    public byte[] DecodeIcoBmpToRgba(ReadOnlySpan<byte> data, BmpInfoHeader header)
    {
        var width = header.Width;
        var height = header.Height / 2;
        var rgbaData = new byte[width * height * 4];

        var dataOffset = header.CalculateDataOffset();
        var pixelStride = BmpLayout.Stride(width * 24);
        var maskStride = BmpLayout.MaskStride(width);
        var maskOffset = dataOffset + (pixelStride * height);
        var hasMask = BmpLayout.HasMask(data, maskOffset, width, height);

        for (var y = 0; y < height; y++)
        {
            var rowOffset = dataOffset + (y * pixelStride);

            // ICO stores rows bottom-up, so the last row read is the top row of the image.
            var targetRow = height - 1 - y;

            for (var x = 0; x < width; x++)
            {
                var source = rowOffset + (x * BytesPerPixel);
                var pixelIndex = ((targetRow * width) + x) * 4;

                // Bitmaps store colour as BGR.
                rgbaData[pixelIndex] = data[source + 2];
                rgbaData[pixelIndex + 1] = data[source + 1];
                rgbaData[pixelIndex + 2] = data[source];

                if (!hasMask || !BmpLayout.IsTransparent(data, maskOffset, maskStride, y, x))
                    rgbaData[pixelIndex + 3] = 255;
            }
        }

        return rgbaData;
    }
}
