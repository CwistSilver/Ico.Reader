using Ico.Reader.Data;

namespace Ico.Reader.Decoder.ImageDecoder;

/// <summary>
/// Defines the functionality for a generic image decoder, supporting operations to decode image data, read image metadata, and check format support.
/// </summary>
public interface IDecoder
{
    /// <summary>
    /// Gets the image format supported by this decoder. This property indicates the specific format that the decoder can process.
    /// </summary>
    IcoImageFormat SupportedFormat { get; }

    /// <summary>
    /// Decodes the provided image data into a byte array. This method is used to convert image data into a raw byte format, suitable for further processing or display.
    /// </summary>
    /// <param name="data">The raw image data to decode.</param>
    /// <returns>A byte array representing the decoded image.</returns>
    byte[] Decode(ReadOnlySpan<byte> data);

    /// <summary>
    /// Reads and returns the metadata associated with the image, such as dimensions and color depth, without fully decoding the image content.
    /// This method is useful for obtaining image information without the overhead of a full decode.
    /// </summary>
    /// <param name="data">The raw image data from which to read metadata.</param>
    /// <returns>An <see cref="ImageReference"/> containing the image metadata, or null if the metadata cannot be read.</returns>
    ImageReference? ReadImageMetadata(ReadOnlySpan<byte> data);

    /// <summary>
    /// Checks whether the provided image data is in a format supported by this decoder.
    /// </summary>
    /// <param name="data">The raw image data to check for format compatibility.</param>
    /// <returns><c>true</c> if the data is in a supported format; otherwise, <c>false</c>.</returns>
    bool IsSupported(ReadOnlySpan<byte> data);
}