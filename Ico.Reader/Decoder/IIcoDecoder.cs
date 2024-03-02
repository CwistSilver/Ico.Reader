using Ico.Reader.Data;

namespace Ico.Reader.Decoder;
/// <summary>
/// Defines the functionality for decoding images from ico file format.
/// </summary>
public interface IIcoDecoder
{
    /// <summary>
    /// Extracts the image data from ico data in a specified format, converting it into a raw byte array suitable for rendering or further processing.
    /// </summary>
    /// <param name="imageData">The raw ico image data to be decoded.</param>
    /// <param name="format">The format of the ico image data to guide the decoding process.</param>
    /// <returns>A byte array containing the decoded image data.</returns>
    byte[] GetImageData(ReadOnlySpan<byte> imageData, IcoImageFormat format);

    /// <summary>
    /// Reads and returns the metadata associated with an ico image, such as dimensions and color depth, without decoding the entire image.
    /// This method provides a means to access essential information about an ico image efficiently.
    /// </summary>
    /// <param name="imageData">The raw ico image data from which to read metadata.</param>
    /// <returns>An <see cref="ImageReference"/> object containing the image metadata, or null if the metadata cannot be extracted.</returns>
    ImageReference? ReadImageMetadata(ReadOnlySpan<byte> imageData);

    /// <summary>
    /// Determines the format of the provided ico image data by analyzing its contents.
    /// This method allows for the identification of the ico format, which is crucial for selecting the appropriate decoding strategy.
    /// </summary>
    /// <param name="imageData">The raw ico image data to be analyzed.</param>
    /// <returns>The <see cref="IcoImageFormat"/> representing the format of the ico image.</returns>
    IcoImageFormat ReadFormat(ReadOnlySpan<byte> imageData);
}
