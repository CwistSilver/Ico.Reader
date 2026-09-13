using Ico.Reader.Data;

namespace Ico.Reader.Decoder.ImageDecoder.Bmp;

/// <summary>
/// Decodes 32 bit per pixel BMP icon data, using the alpha channel in the pixels themselves.
/// </summary>
/// <remarks>
/// An alpha channel that is zero throughout counts as absent, as it does for Windows, and the AND mask decides
/// transparency instead.
/// </remarks>
public sealed class IcoBmp32Decoder : IIcoBmpDecoder
{
    private const int BytesPerPixel = 4;

    /// <inheritdoc/>
    public byte BitCountSupported => 32;

    /// <inheritdoc/>
    public byte[] DecodeIcoBmpToRgba(ReadOnlySpan<byte> data, BmpInfoHeader header)
    {
        var width = header.Width;
        var height = header.Height / 2;
        var pixels = new byte[width * height * BytesPerPixel];
        var offset = header.CalculateDataOffset();

        var usesMask = !HasAlpha(data.Slice(offset, width * height * BytesPerPixel));
        var maskOffset = offset + (BmpLayout.Stride(width * 32) * height);
        var maskStride = BmpLayout.MaskStride(width);
        var hasMask = usesMask && BmpLayout.HasMask(data, maskOffset, width, height);

        for (var y = height - 1; y >= 0; y--)
        {
            // ICO stores rows bottom-up, so row y of the image is the mirrored row of the data.
            var sourceRow = height - 1 - y;

            for (var x = 0; x < width; x++)
            {
                var i = offset + (((sourceRow * width) + x) * BytesPerPixel);

                var pixelIndex = ((y * width) + x) * BytesPerPixel;

                pixels[pixelIndex] = data[i + 2];
                pixels[pixelIndex + 1] = data[i + 1];
                pixels[pixelIndex + 2] = data[i];
                pixels[pixelIndex + 3] = usesMask
                    ? MaskAlpha(data, hasMask, maskOffset, maskStride, sourceRow, x)
                    : data[i + 3];
            }
        }

        return pixels;
    }

    private static bool HasAlpha(ReadOnlySpan<byte> pixelData)
    {
        for (var i = 3; i < pixelData.Length; i += BytesPerPixel)
        {
            if (pixelData[i] != 0)
                return true;
        }

        return false;
    }

    private static byte MaskAlpha(ReadOnlySpan<byte> data, bool hasMask, int maskOffset, int maskStride, int row, int x)
        => hasMask && BmpLayout.IsTransparent(data, maskOffset, maskStride, row, x) ? (byte)0 : (byte)255;
}
