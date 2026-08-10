using System.Buffers.Binary;

using Ico.Reader.Data;

namespace Ico.Reader.Decoder.ImageDecoder;
/// <summary>
/// A decoder that specializes in decoding PNG (Portable Network Graphics) image data from ico files.
/// This decoder interprets PNG data embedded within ico files, allowing for the extraction and manipulation of PNG images.
/// </summary>
public sealed class PngDecoder : IDecoder
{
    private static readonly byte[] _pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>
    /// Specifies that this decoder supports the PNG image format.
    /// </summary>
    public IcoImageFormat SupportedFormat => IcoImageFormat.PNG;

    /// <summary>
    /// Decodes PNG image data into a byte array. This implementation simply returns the input data as PNG images are already in a suitable format for most applications.
    /// </summary>
    /// <param name="data">The PNG image data to decode.</param>
    /// <returns>A byte array containing the PNG image data.</returns>
    public byte[] Decode(ReadOnlySpan<byte> data) => data.ToArray();

    /// <summary>
    /// Determines if the provided data is in the PNG format by checking the signature.
    /// </summary>
    /// <param name="data">The image data to check.</param>
    /// <returns>True if the data is in the PNG format; otherwise, false.</returns>
    public bool IsSupported(ReadOnlySpan<byte> data) => data.Slice(0, 8).SequenceEqual(_pngSignature);

    /// <summary>
    /// Reads and returns metadata from PNG image data.
    /// </summary>
    /// <param name="data">The PNG image data to analyze.</param>
    /// <returns>An <see cref="ImageReference"/> object containing metadata about the PNG image, or null if the data format is not supported.</returns>
    public ImageReference? ReadImageMetadata(ReadOnlySpan<byte> data)
    {
        return new ImageReference
        {
            Width = BinaryPrimitives.ReadInt32BigEndian(data.Slice(16, 4)),
            Height = BinaryPrimitives.ReadInt32BigEndian(data.Slice(20, 4)),
            BitCount = ReadBitCount(data),
            Format = SupportedFormat
        };
    }

    private static int ReadBitCount(ReadOnlySpan<byte> data)
    {
        int bitDepth = data[24];
        int colorType = data[25];

        return bitDepth * SamplesPerPixel(colorType);
    }

    /// <summary>
    /// Maps an IHDR colour type to the number of samples it stores per pixel.
    /// <para> Reference: <see href="https://www.w3.org/TR/png/#6Colour-values">PNG Specification, Colour values</see> </para>
    /// </summary>
    private static int SamplesPerPixel(int colorType) => colorType switch
    {
        0 => 1, // Greyscale
        2 => 3, // Truecolour
        3 => 1, // Indexed-colour
        4 => 2, // Greyscale with alpha
        6 => 4, // Truecolour with alpha
        _ => 0,
    };
}
