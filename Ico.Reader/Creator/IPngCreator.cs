using Ico.Reader.Data;

namespace Ico.Reader.Creator;

/// <summary>
/// Wraps decoded pixel data in a PNG container, which is the format every decoder returns.
/// </summary>
public interface IPngCreator
{
    /// <summary>
    /// Encodes RGBA pixels as a PNG file.
    /// </summary>
    /// <param name="rgba">The pixels, four bytes each, in top-down row order.</param>
    /// <param name="header">The bitmap header the pixels were decoded from, which supplies the
    /// dimensions. Its height covers both the colour and mask planes, so the image is half of it.</param>
    /// <returns>The bytes of a complete PNG file.</returns>
    byte[] CreatePng(ReadOnlySpan<byte> rgba, BmpInfoHeader header);
}