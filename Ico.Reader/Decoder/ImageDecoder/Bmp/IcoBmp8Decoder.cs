using Ico.Reader.Data;
using System.Drawing;

namespace Ico.Reader.Decoder.ImageDecoder.Bmp;
public sealed class IcoBmp8Decoder : IIcoBmpDecoder
{
    public byte BitCountSupported => 8;

    public byte[] DecodeIcoBmpToRgba(ReadOnlySpan<byte> data, BMP_Info_Header header)
    {
        int width = header.Width;
        int height = header.Height / 2;

        Color[] palette = CreateColorPalette(data, header);
        byte[] argbData = new byte[width * height * 4];

        int bytesPerRowImage = width;
        int imageRowPadding = (4 - (bytesPerRowImage % 4)) % 4;
        int totalImageSize = (bytesPerRowImage + imageRowPadding) * height;

        int dataOffset = header.CalculateDataOffset();
        int maskOffset = dataOffset + totalImageSize;

        int maskRowBytesActual = (width + 7) / 8;
        int maskPadding = (4 - (maskRowBytesActual % 4)) % 4;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int pixelIndex = ((height - 1 - y) * width + x) * 4;
                int dataRowOffset = dataOffset + y * (width + imageRowPadding);
                byte paletteIndex = data[dataRowOffset + x];

                argbData[pixelIndex] = palette[paletteIndex].R;
                argbData[pixelIndex + 1] = palette[paletteIndex].G;
                argbData[pixelIndex + 2] = palette[paletteIndex].B;

                int maskByteIndex = maskOffset + y * (maskRowBytesActual + maskPadding) + x / 8;
                int maskBit = 7 - (x % 8);
                bool isTransparent = ((data[maskByteIndex] >> maskBit) & 1) == 1;

                if (!isTransparent)
                    argbData[pixelIndex + 3] = 255;
            }
        }

        return argbData;
    }

    private static Color[] CreateColorPalette(ReadOnlySpan<byte> data, BMP_Info_Header header)
    {
        int paletteSize = header.CalculatePaletteSize();
        Color[] palette = new Color[paletteSize];
        int paletteOffset = header.Size;

        for (int i = 0; i < paletteSize; i++)
        {
            byte blue = data[paletteOffset + i * 4];
            byte green = data[paletteOffset + i * 4 + 1];
            byte red = data[paletteOffset + i * 4 + 2];

            palette[i] = Color.FromArgb(255, red, green, blue);
        }

        return palette;
    }
}
