using System.Drawing;

using Ico.Reader.Data;

namespace Ico.Reader.Decoder.ImageDecoder.Bmp;

/// <summary>
/// Decodes the palette-based ICO bitmap depths. Everything except unpacking a palette index from a
/// row is identical between them, so only <see cref="ReadPaletteIndex"/> differs per depth.
/// </summary>
public abstract class IndexedBmpDecoder : IIcoBmpDecoder
{
    /// <inheritdoc/>
    public abstract byte BitCountSupported { get; }

    /// <summary>
    /// Reads the palette index of pixel <paramref name="x"/> from one padded row of pixel data.
    /// </summary>
    protected abstract byte ReadPaletteIndex(ReadOnlySpan<byte> row, int x);

    /// <inheritdoc/>
    public byte[] DecodeIcoBmpToRgba(ReadOnlySpan<byte> data, BmpInfoHeader header)
    {
        var width = header.Width;
        var height = header.Height / 2;
        var palette = CreateColorPalette(data, header);
        var rgbaData = new byte[width * height * 4];

        var dataOffset = header.CalculateDataOffset();
        var pixelStride = BmpLayout.Stride(width * BitCountSupported);
        var maskStride = BmpLayout.MaskStride(width);
        var maskOffset = dataOffset + (pixelStride * height);
        var hasMask = BmpLayout.HasMask(data, maskOffset, width, height);

        var allTransparent = true;

        for (var y = 0; y < height; y++)
        {
            var row = data.Slice(dataOffset + (y * pixelStride), pixelStride);

            // ICO stores rows bottom-up, so the last row read is the top row of the image.
            var targetRow = height - 1 - y;

            for (var x = 0; x < width; x++)
            {
                var color = palette[ReadPaletteIndex(row, x)];
                var pixelIndex = ((targetRow * width) + x) * 4;

                rgbaData[pixelIndex] = color.R;
                rgbaData[pixelIndex + 1] = color.G;
                rgbaData[pixelIndex + 2] = color.B;

                if (hasMask && BmpLayout.IsTransparent(data, maskOffset, maskStride, y, x))
                    continue;

                rgbaData[pixelIndex + 3] = 255;
                allTransparent = false;
            }
        }

        if (allTransparent)
            MakeImageVisible(rgbaData, palette);

        return rgbaData;
    }

    /// <summary>
    /// Recolours an image whose mask hides every pixel, so that it renders as something rather than
    /// as nothing. Each pixel swaps to the other end of the palette and becomes opaque unless it was
    /// already the transparent colour.
    /// </summary>
    private static void MakeImageVisible(Span<byte> rgbaData, Color[] palette)
    {
        var transparentColor = palette[0];

        for (var i = 0; i < rgbaData.Length; i += 4)
        {
            var currentColorIndex = FindColorIndex(rgbaData, i, palette);
            var isVisible = palette[currentColorIndex] != transparentColor;
            var newColor = palette[currentColorIndex == 0 ? 1 : 0];

            rgbaData[i] = newColor.R;
            rgbaData[i + 1] = newColor.G;
            rgbaData[i + 2] = newColor.B;

            if (isVisible)
                rgbaData[i + 3] = 255;
        }
    }

    private static byte FindColorIndex(ReadOnlySpan<byte> rgbaData, int startIndex, Color[] palette)
    {
        for (var i = 0; i < palette.Length; i++)
        {
            if (rgbaData[startIndex] == palette[i].R && rgbaData[startIndex + 1] == palette[i].G && rgbaData[startIndex + 2] == palette[i].B)
                return (byte)i;
        }

        throw new InvalidDataException("The pixel colour is not present in the palette.");
    }

    private static Color[] CreateColorPalette(ReadOnlySpan<byte> data, BmpInfoHeader header)
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
