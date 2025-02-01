using Ico.Reader.Data;
using System.Drawing;

namespace Ico.Reader.Decoder.ImageDecoder.Bmp;
public sealed class IcoBmp4Decoder : IIcoBmpDecoder
{
    public byte BitCountSupported => 4;
    public byte[] DecodeIcoBmpToRgba(ReadOnlySpan<byte> data, BMP_Info_Header header)
    {
        int width = header.Width;
        int height = header.Height / 2;

        Color[] palette = CreateColorPalette(data, header);
        byte[] rgbaData = new byte[width * height * 4];

        int dataOffset = header.CalculateDataOffset();

        int bytesPerRow = (width + 1) / 2;
        int paddedBytesPerRow = (bytesPerRow + 3) & ~3;
        int maskOffset = dataOffset + paddedBytesPerRow * height;

        int maskRowBytes = (width + 7) / 8;
        int maskPadding = (4 - (maskRowBytes % 4)) % 4;
        bool allTransparent = true;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int dataIndex = dataOffset + y * paddedBytesPerRow + (x / 2);
                bool isHighNibble = x % 2 == 0;
                byte nibbleValue = isHighNibble ? (byte)(data[dataIndex] >> 4) : (byte)(data[dataIndex] & 0x0F);

                int pixelIndex = ((height - 1 - y) * width + x) * 4;
                bool isTransparent = IsPixelTransparent(x, height - 1 - y, data, maskOffset, maskRowBytes, maskPadding, height);

                rgbaData[pixelIndex] = palette[nibbleValue].R;
                rgbaData[pixelIndex + 1] = palette[nibbleValue].G;
                rgbaData[pixelIndex + 2] = palette[nibbleValue].B;

                if (!isTransparent)
                {
                    rgbaData[pixelIndex + 3] = 255;
                    allTransparent = false;
                }
            }
        }

        if (allTransparent)
            MakeImageVisible(rgbaData, ref palette);

        return rgbaData;
    }

    private static void MakeImageVisible(Span<byte> rgbaData, ref Color[] palette)
    {
        var transparentColor = palette[0];
        for (int i = 0; i < rgbaData.Length; i += 4)
        {
            byte currentColorIndex = FindColorIndex(rgbaData, ref i, ref palette);
            bool isVisible = palette[currentColorIndex] != transparentColor;
            var newColor = palette[currentColorIndex == 0 ? 1 : 0];
            rgbaData[i + 0] = newColor.R;
            rgbaData[i + 1] = newColor.G;
            rgbaData[i + 2] = newColor.B;

            if (isVisible)
            {
                rgbaData[i + 3] = 255;
            }
        }
    }

    private static byte FindColorIndex(ReadOnlySpan<byte> rgbaData, ref int startIndex, ref Color[] palette)
    {
        for (int i = 0; i < palette.Length; i++)
        {
            if (rgbaData[startIndex] == palette[i].R && rgbaData[startIndex + 1] == palette[i].G && rgbaData[startIndex + 2] == palette[i].B)
            {
                return (byte)i;
            }
        }

        throw new Exception("Color not found");
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
