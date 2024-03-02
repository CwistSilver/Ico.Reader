using Ico.Reader.Data;

namespace Ico.Reader.Decoder;
/// <summary>
/// Defines functionality for decoding ico data from pe files.
/// </summary>
public interface IIcoPeDecoder
{
    /// <summary>
    /// Decodes ico data from the given stream, using the specified ico decoder to process the data.
    /// </summary>
    /// <param name="stream">The stream containing executable data that may include icos.</param>
    /// <param name="icoDecoder">The ico decoder to use for processing the ico data.</param>
    /// <returns>A DecodedIcoResult containing the decoded icos and their metadata, or null if decoding fails.</returns>
    DecodedIcoResult? GetDecodedIcoResult(Stream stream, IIcoDecoder icoDecoder);

    /// <summary>
    /// Checks if the given stream represents an pe file that potentially contains ico data.
    /// </summary>
    /// <param name="stream">The stream to check.</param>
    /// <returns>True if the stream represents an pe file; otherwise, false.</returns>
    bool IsPeFormat(Stream stream);
}