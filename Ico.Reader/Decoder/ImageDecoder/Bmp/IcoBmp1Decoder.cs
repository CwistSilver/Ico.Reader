using Ico.Reader.Data;
using System.Drawing;

namespace Ico.Reader.Decoder.ImageDecoder.Bmp;
public sealed class IcoBmp1Decoder : IIcoBmpDecoder
{
    public byte BitCountSupported => 1;

    public byte[] DecodeIcoBmpToRgba(ReadOnlySpan<byte> data, BMP_Info_Header header)
    {
        int width = header.Width;
        int height = header.Height / 2;

        byte[] rgbaData = new byte[width * height * 4];
        Color[] palette = CreateColorPalette(data, header);

        int dataOffset = header.CalculateDataOffset();
        int bytesPerRowImage = (width + 7) / 8;
        int imageRowPadding = (4 - (bytesPerRowImage % 4)) % 4;
        int totalImageSize = (bytesPerRowImage + imageRowPadding) * height;

        int maskOffset = dataOffset + totalImageSize;
        bool allTransparent = true;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int pixelIndex = ((height - 1 - y) * width + x) * 4;
                int byteIndex = dataOffset + y * (bytesPerRowImage + imageRowPadding) + x / 8;
                int bitIndex = 7 - (x % 8);

                bool isSet = ((data[byteIndex] >> bitIndex) & 1) == 1;


                int maskByteIndex = maskOffset + y * (bytesPerRowImage + imageRowPadding) + x / 8;
                bool isTransparent = ((data[maskByteIndex] >> bitIndex) & 1) == 1;

                rgbaData[pixelIndex] = isSet ? palette[1].R : palette[0].R;
                rgbaData[pixelIndex + 1] = isSet ? palette[1].G : palette[0].G;
                rgbaData[pixelIndex + 2] = isSet ? palette[1].B : palette[0].B;

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
