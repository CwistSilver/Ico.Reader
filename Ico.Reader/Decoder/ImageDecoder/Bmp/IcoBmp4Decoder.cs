using System.Drawing;
using Ico.Reader.Data;

namespace Ico.Reader.Decoder.ImageDecoder.Bmp;
public sealed class IcoBmp4Decoder : IIcoBmpDecoder
{
    public byte BitCountSupported => 4;

    public byte[] DecodeIcoBmpToRgba(ReadOnlySpan<byte> data, BMP_Info_Header header)
    {
        int width = header.Width;
        int height = header.Height / 2;

        Color[] palette = CreateColorPalette(data, header);
        byte[] argbData = new byte[width * height * 4];

        int dataOffset = header.CalculateDataOffset();

        int bytesPerRow = (width + 1) / 2;
        int paddedBytesPerRow = (bytesPerRow + 3) & ~3;
        int maskOffset = dataOffset + paddedBytesPerRow * height;

        int maskRowBytes = (width + 7) / 8;
        int maskPadding = (4 - (maskRowBytes % 4)) % 4;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int dataIndex = dataOffset + y * paddedBytesPerRow + (x / 2);
                bool isHighNibble = x % 2 == 0;
                byte nibbleValue = isHighNibble ? (byte)(data[dataIndex] >> 4) : (byte)(data[dataIndex] & 0x0F);

                int pixelIndex = ((height - 1 - y) * width + x) * 4;
                bool isTransparent = IsPixelTransparent(x, height - 1 - y, data, maskOffset, maskRowBytes, maskPadding, height);
                           
                argbData[pixelIndex] = palette[nibbleValue].R;
                argbData[pixelIndex + 1] = palette[nibbleValue].G;
                argbData[pixelIndex + 2] = palette[nibbleValue].B;

                if (!isTransparent)
                    argbData[pixelIndex + 3] = 255;
            }
        }

        return argbData;
    }



    private static bool IsPixelTransparent(int x, int y, ReadOnlySpan<byte> data, int maskOffset, int maskRowBytes, int maskPadding, int height)
    {
        int maskY = height - 1 - y;
        int maskByteIndex = maskOffset + maskY * (maskRowBytes + maskPadding) + x / 8;
        int maskBit = 7 - x % 8;
        return (data[maskByteIndex] >> maskBit & 1) == 1;
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
