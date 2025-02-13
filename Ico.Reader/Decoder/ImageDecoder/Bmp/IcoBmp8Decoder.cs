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
        byte[] rgbaData = new byte[width * height * 4];

        int bytesPerRowImage = width;
        int imageRowPadding = (4 - (bytesPerRowImage % 4)) % 4;
        int totalImageSize = (bytesPerRowImage + imageRowPadding) * height;

        int dataOffset = CalculateDataOffset(header);
        int maskOffset = dataOffset + totalImageSize;

        int maskRowBytesActual = (width + 7) / 8;
        int maskPadding = (4 - (maskRowBytesActual % 4)) % 4;
        bool allTransparent = true;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int pixelIndex = ((height - 1 - y) * width + x) * 4;
                int dataRowOffset = dataOffset + y * (width + imageRowPadding);

                byte paletteIndex = data[dataRowOffset + x];

                rgbaData[pixelIndex] = palette[paletteIndex].R;
                rgbaData[pixelIndex + 1] = palette[paletteIndex].G;
                rgbaData[pixelIndex + 2] = palette[paletteIndex].B;

                int maskByteIndex = maskOffset + y * (maskRowBytesActual + maskPadding) + x / 8;
                int maskBit = 7 - (x % 8);
                bool isTransparent = true;
                if (data.Length > maskByteIndex)
                {
                    isTransparent = ((data[maskByteIndex] >> maskBit) & 1) == 1;
                }

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

    public int CalculateDataOffset(BMP_Info_Header header) => header.Size + (header.ClrUsed > 0 ? header.ClrUsed : (1 << header.BitCount)) * 4;


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
