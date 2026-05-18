using System.Drawing;

using Ico.Reader.Data;

namespace Ico.Reader.Decoder.ImageDecoder.Bmp;

public sealed class IcoBmp1Decoder : IIcoBmpDecoder
{
    public byte BitCountSupported => 1;

    public byte[] DecodeIcoBmpToRgba(ReadOnlySpan<byte> data, BMP_Info_Header header)
    {
        var width = header.Width;
        var height = header.Height / 2;

        var rgbaData = new byte[width * height * 4];
        var palette = CreateColorPalette(data, header);

        var dataOffset = header.CalculateDataOffset();
        var bytesPerRowImage = (width + 7) / 8;
        var imageRowPadding = (4 - (bytesPerRowImage % 4)) % 4;
        var totalImageSize = (bytesPerRowImage + imageRowPadding) * height;

        var maskOffset = dataOffset + totalImageSize;
        var allTransparent = true;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixelIndex = (((height - 1 - y) * width) + x) * 4;
                var byteIndex = dataOffset + (y * (bytesPerRowImage + imageRowPadding)) + (x / 8);
                var bitIndex = 7 - (x % 8);

                var isSet = ((data[byteIndex] >> bitIndex) & 1) == 1;

                var maskByteIndex = maskOffset + (y * (bytesPerRowImage + imageRowPadding)) + (x / 8);
                var isTransparent = ((data[maskByteIndex] >> bitIndex) & 1) == 1;

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
        for (var i = 0; i < rgbaData.Length; i += 4)
        {
            var currentColorIndex = FindColorIndex(rgbaData, ref i, ref palette);
            var isVisible = palette[currentColorIndex] != transparentColor;
            var newColor = palette[currentColorIndex == 0 ? 1 : 0];
            rgbaData[i + 0] = newColor.R;
            rgbaData[i + 1] = newColor.G;
            rgbaData[i + 2] = newColor.B;

            if (isVisible)
                rgbaData[i + 3] = 255;
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
