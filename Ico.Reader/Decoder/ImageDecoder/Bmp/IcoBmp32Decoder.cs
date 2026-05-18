using Ico.Reader.Data;

namespace Ico.Reader.Decoder.ImageDecoder.Bmp;

public sealed class IcoBmp32Decoder : IIcoBmpDecoder
{
    public byte BitCountSupported => 32;

    public byte[] DecodeIcoBmpToRgba(ReadOnlySpan<byte> data, BMP_Info_Header header)
    {
        var width = header.Width;
        var height = header.Height / 2;
        var pixels = new byte[width * height * 4];
        var offset = header.Size + (header.ClrUsed * 4);

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
