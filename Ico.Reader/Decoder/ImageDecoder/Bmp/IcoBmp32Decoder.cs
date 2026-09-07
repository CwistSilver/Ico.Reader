using Ico.Reader.Data;

namespace Ico.Reader.Decoder.ImageDecoder.Bmp;

/// <summary>
/// Decodes 32 bit per pixel BMP icon data, using the alpha channel in the pixels themselves.
/// </summary>
public sealed class IcoBmp32Decoder : IIcoBmpDecoder
{
    /// <inheritdoc/>
    public byte BitCountSupported => 32;

    /// <inheritdoc/>
    public byte[] DecodeIcoBmpToRgba(ReadOnlySpan<byte> data, BmpInfoHeader header)
    {
        var width = header.Width;
        var height = header.Height / 2;
        var pixels = new byte[width * height * 4];
        var offset = header.CalculateDataOffset();

        for (var y = height - 1; y >= 0; y--)
        {
            for (var x = 0; x < width; x++)
            {
                var i = offset + ((((height - 1 - y) * width) + x) * 4);

                var pixelIndex = ((y * width) + x) * 4;

                pixels[pixelIndex] = data[i + 2];
                pixels[pixelIndex + 1] = data[i + 1];
                pixels[pixelIndex + 2] = data[i];
                pixels[pixelIndex + 3] = data[i + 3];
            }
        }

        return pixels;
    }
}
