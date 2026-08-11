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
    /// Reads the AND mask bit for a pixel, where a set bit means the pixel is transparent.
    /// </summary>
    /// <remarks>
    /// A truncated file can end before the mask does. Treating the missing part as transparent keeps
    /// the image readable, and a fully transparent result is later made visible again by
    /// <see cref="IndexedBmpDecoder"/>.
    /// </remarks>
    public static bool IsTransparent(ReadOnlySpan<byte> data, int maskOffset, int maskStride, int row, int x)
    {
        var maskByteIndex = maskOffset + (row * maskStride) + (x / 8);
        if (maskByteIndex >= data.Length)
            return true;

        var maskBit = 7 - (x % 8);
        return ((data[maskByteIndex] >> maskBit) & 1) == 1;
    }
}
