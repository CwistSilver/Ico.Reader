using Ico.Reader.Data;

namespace Ico.Reader.Decoder.ImageDecoder.Bmp;
/// <summary>
/// Defines the functionality for an ico bitmap decoder that converts bitmap data from icos into an array of ARGB values.
/// This interface specifies the supported bit depth for decoding and a method for the actual decoding process.
/// </summary>
public interface IIcoBmpDecoder
{
    /// <summary>
    /// Gets the bit depth supported by this decoder. Icos with a bit depth not matching this value cannot be decoded by this decoder.
    /// </summary>
    byte BitCountSupported { get; }

    /// <summary>
    /// Decodes the bitmap data from an ico into an array of RGBA values, based on the provided bitmap information header.
    /// This method allows for the conversion of bitmap data into a format suitable for direct manipulation and display in applications.
    /// </summary>
    /// <param name="data">The raw bitmap data to decode. This data should correspond to the bitmap information provided in the header.</param>
    /// <param name="header">The header providing details about the bitmap data, such as dimensions and bit depth.</param>
    /// <returns>An array of bytes representing the decoded bitmap in RGBA format.</returns>
    byte[] DecodeIcoBmpToRgba(ReadOnlySpan<byte> data, BMP_Info_Header header);
}
