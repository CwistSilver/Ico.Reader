namespace Ico.Reader.Decoder.ImageDecoder.Bmp;

/// <summary>
/// The row geometry shared by every ICO bitmap: pixel rows padded to a four byte boundary, followed
/// by a 1-bit AND mask with the same padding rule.
/// </summary>
internal static class BmpLayout
{
    /// <summary>
    /// The byte length of one padded row holding <paramref name="bitsPerRow"/> bits of data.
    /// </summary>
    public static int Stride(int bitsPerRow) => (bitsPerRow + 31) / 32 * 4;

    /// <summary>
    /// The byte length of one row of the 1-bit AND mask.
    /// </summary>
    public static int MaskStride(int width) => Stride(width);

    /// <summary>
    /// Reports whether the data holds the whole AND mask.
    /// </summary>
    /// <remarks>
    /// Some writers leave the mask out. Windows draws every pixel of such an image, and ignores a mask the data cuts
    /// short in the same way, so a partial mask counts as none.
    /// </remarks>
    public static bool HasMask(ReadOnlySpan<byte> data, int maskOffset, int width, int height)
        => maskOffset + ((long)MaskStride(width) * height) <= data.Length;

    /// <summary>
    /// Reads the AND mask bit for a pixel, where a set bit means the pixel is transparent. Only valid once
    /// <see cref="HasMask"/> has confirmed the data holds the mask.
    /// </summary>
    public static bool IsTransparent(ReadOnlySpan<byte> data, int maskOffset, int maskStride, int row, int x)
    {
        var maskByteIndex = maskOffset + (row * maskStride) + (x / 8);
        var maskBit = 7 - (x % 8);

        return ((data[maskByteIndex] >> maskBit) & 1) == 1;
    }
}
