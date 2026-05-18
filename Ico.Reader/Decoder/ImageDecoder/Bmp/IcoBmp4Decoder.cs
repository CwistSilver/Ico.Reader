using System.Drawing;

using Ico.Reader.Data;

namespace Ico.Reader.Decoder.ImageDecoder.Bmp;

public sealed class IcoBmp4Decoder : IIcoBmpDecoder
{
    public byte BitCountSupported => 4;
    public byte[] DecodeIcoBmpToRgba(ReadOnlySpan<byte> data, BMP_Info_Header header)
    {
        var width = header.Width;
        var height = header.Height / 2;

        var palette = CreateColorPalette(data, header);
        var rgbaData = new byte[width * height * 4];

        var dataOffset = header.CalculateDataOffset();

        var bytesPerRow = (width + 1) / 2;
        var paddedBytesPerRow = (bytesPerRow + 3) & ~3;
        var maskOffset = dataOffset + (paddedBytesPerRow * height);

        var maskRowBytes = (width + 7) / 8;
        var maskPadding = (4 - (maskRowBytes % 4)) % 4;
        var allTransparent = true;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var dataIndex = dataOffset + (y * paddedBytesPerRow) + (x / 2);
                var isHighNibble = x % 2 == 0;
                var nibbleValue = isHighNibble ? (byte)(data[dataIndex] >> 4) : (byte)(data[dataIndex] & 0x0F);

                var pixelIndex = (((height - 1 - y) * width) + x) * 4;
                var isTransparent = IsPixelTransparent(x, height - 1 - y, data, maskOffset, maskRowBytes, maskPadding, height);

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
        for (var i = 0; i < rgbaData.Length; i += 4)
        {
            var currentColorIndex = FindColorIndex(rgbaData, ref i, ref palette);
            var isVisible = palette[currentColorIndex] != transparentColor;
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
        for (var i = 0; i < palette.Length; i++)
        {
            if (rgbaData[startIndex] == palette[i].R && rgbaData[startIndex + 1] == palette[i].G && rgbaData[startIndex + 2] == palette[i].B)
                return (byte)i;
        }

        throw new Exception("Color not found");
    }

    private static bool IsPixelTransparent(int x, int y, ReadOnlySpan<byte> data, int maskOffset, int maskRowBytes, int maskPadding, int height)
    {
        var maskY = height - 1 - y;
        var maskByteIndex = maskOffset + (maskY * (maskRowBytes + maskPadding)) + (x / 8);
        var maskBit = 7 - (x % 8);
        return ((data[maskByteIndex] >> maskBit) & 1) == 1;
    }

    private static Color[] CreateColorPalette(ReadOnlySpan<byte> data, BMP_Info_Header header)
    {
        var paletteSize = header.CalculatePaletteSize();
        var palette = new Color[paletteSize];
        var paletteOffset = header.Size;

        for (var i = 0; i < paletteSize; i++)
        {
            var blue = data[paletteOffset + (i * 4)];
            var green = data[paletteOffset + (i * 4) + 1];
            var red = data[paletteOffset + (i * 4) + 2];

            palette[i] = Color.FromArgb(255, red, green, blue);
        }

        return palette;
    }
}
