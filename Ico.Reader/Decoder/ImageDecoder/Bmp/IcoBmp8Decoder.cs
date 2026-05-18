using System.Drawing;

using Ico.Reader.Data;

namespace Ico.Reader.Decoder.ImageDecoder.Bmp;

public sealed class IcoBmp8Decoder : IIcoBmpDecoder
{
    public byte BitCountSupported => 8;

    public byte[] DecodeIcoBmpToRgba(ReadOnlySpan<byte> data, BMP_Info_Header header)
    {
        var width = header.Width;
        var height = header.Height / 2;

        var palette = CreateColorPalette(data, header);
        var rgbaData = new byte[width * height * 4];

        var bytesPerRowImage = width;
        var imageRowPadding = (4 - (bytesPerRowImage % 4)) % 4;
        var totalImageSize = (bytesPerRowImage + imageRowPadding) * height;

        var dataOffset = CalculateDataOffset(header);
        var maskOffset = dataOffset + totalImageSize;

        var maskRowBytesActual = (width + 7) / 8;
        var maskPadding = (4 - (maskRowBytesActual % 4)) % 4;
        var allTransparent = true;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixelIndex = (((height - 1 - y) * width) + x) * 4;
                var dataRowOffset = dataOffset + (y * (width + imageRowPadding));

                var paletteIndex = data[dataRowOffset + x];

                rgbaData[pixelIndex] = palette[paletteIndex].R;
                rgbaData[pixelIndex + 1] = palette[paletteIndex].G;
                rgbaData[pixelIndex + 2] = palette[paletteIndex].B;

                var maskByteIndex = maskOffset + (y * (maskRowBytesActual + maskPadding)) + (x / 8);
                var maskBit = 7 - (x % 8);
                var isTransparent = true;
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
