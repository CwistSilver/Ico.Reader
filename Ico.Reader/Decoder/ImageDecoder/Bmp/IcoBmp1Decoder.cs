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

        byte[] argbData = new byte[width * height * 4];
        Color[] palette = CreateColorPalette(data, header);

        int dataOffset = header.CalculateDataOffset();

        int bytesPerRowImage = (width + 7) / 8;
        int imageRowPadding = (4 - (bytesPerRowImage % 4)) % 4;
        int totalImageSize = (bytesPerRowImage + imageRowPadding) * height;

        int maskOffset = dataOffset + totalImageSize;


        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int pixelIndex = ((height - 1 - y) * width + x) * 4;
                int byteIndex = dataOffset + y * (bytesPerRowImage + imageRowPadding) + x / 8;
                int bitIndex = 7 - (x % 8);

                bool isSet = ((data[byteIndex] >> bitIndex) & 1) == 0;

                int maskByteIndex = maskOffset + y * (bytesPerRowImage + imageRowPadding) + x / 8;
                bool isTransparent = ((data[maskByteIndex] >> bitIndex) & 1) == 1;            

                argbData[pixelIndex] = isSet ? palette[1].R : palette[0].R;
                argbData[pixelIndex + 1] = isSet ? palette[1].G : palette[0].G;
                argbData[pixelIndex + 2] = isSet ? palette[1].B : palette[0].B;

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
