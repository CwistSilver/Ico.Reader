namespace Ico.Reader.Data;

/// <summary>
/// Represents the BMP information header.
/// </summary>
public sealed class BmpInfoHeader
{
    /// <summary>
    /// The size of this header in bytes.
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// The width of the bitmap in pixels.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// The height of the bitmap in pixels.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// The number of color planes being used. Must be set to 1.
    /// </summary>
    public ushort Planes { get; set; }

    /// <summary>
    /// The number of bits per pixel, which determines the number of colors that can be represented.
    /// </summary>
    public ushort BitCount { get; set; }

    /// <summary>
    /// The type of compression for a compressed bottom-up bitmap (top-down DIBs cannot be compressed).
    /// </summary>
    public int Compression { get; set; }

    /// <summary>
    /// The size, in bytes, of the image.
    /// </summary>
    public int SizeImage { get; set; }

    /// <summary>
    /// The horizontal resolution of the image, in pixels per meter.
    /// </summary>
    public int XPelsPerMeter { get; set; }

    /// <summary>
    /// The vertical resolution of the image, in pixels per meter.
    /// </summary>
    public int YPelsPerMeter { get; set; }

    /// <summary>
    /// The number of colors in the color palette that are actually used by the bitmap. If this value is 0, all colors are used.
    /// </summary>
    public int ClrUsed { get; set; }

    /// <summary>
    /// The number of important colors used by the bitmap. If this value is 0, all colors are considered important.
    /// </summary>
    public int ClrImportant { get; set; }

    /// <summary>
    /// Calculates the number of colors in the color palette that precedes the bitmap data.
    /// <para>
    /// For indexed bitmaps (8 bits per pixel or fewer), <see cref="ClrUsed"/> gives the palette
    /// length, and a value of 0 means the palette is full. Above 8 bits per pixel a palette is only
    /// present when <see cref="ClrUsed"/> declares one.
    /// </para>
    /// </summary>
    /// <returns>The number of colors in the color palette.</returns>
    public int CalculatePaletteSize()
    {
        if (BitCount > 8)
            return Math.Max(ClrUsed, 0);

        var maxColors = 1 << BitCount;
        return (ClrUsed <= 0 || ClrUsed > maxColors) ? maxColors : ClrUsed;
    }

    /// <summary>
    /// Calculates the offset to the beginning of bitmap data, taking into account the size of the header and the color palette.
    /// </summary>
    /// <returns>The offset to the bitmap data in bytes.</returns>
    public int CalculateDataOffset() => Size + (CalculatePaletteSize() * 4);
}