using Ico.Reader.PeDecoder.Models;
using Ico.Reader.PeDecoder.Reading;

namespace Ico.Reader.PeDecoder;

/// <summary>
/// Reads the headers and the resource tree of a PE file.
/// </summary>
public interface IPeDecoder
{
    /// <summary>
    /// Reads the DOS header from the start of the stream.
    /// </summary>
    /// <param name="stream">The stream to read, positioned anywhere.</param>
    /// <returns>The DOS header.</returns>
    MzHeader DecodeMZ(Stream stream);

    /// <summary>
    /// Reads the COFF header and, when present, the optional header and section table.
    /// </summary>
    /// <param name="stream">The stream to read.</param>
    /// <returns>The PE header.</returns>
    PeHeader DecodePE(Stream stream);

    /// <summary>
    /// Reads the resource tree of an image.
    /// </summary>
    /// <param name="stream">The stream to read.</param>
    /// <param name="peHeader">The header of the same image, as returned by <see cref="DecodePE"/>.</param>
    /// <returns>
    /// The root of the resource tree, or <see langword="null"/> if the image holds no resources.
    /// </returns>
    ResourceDirectory? DecodeResourceDirectory(Stream stream, PeHeader peHeader);

    /// <summary>
    /// Reports whether a DOS header carries the signature every PE file begins with.
    /// </summary>
    /// <param name="mzHeader">The header to inspect.</param>
    /// <returns><see langword="true"/> if the file is a PE file.</returns>
    bool IsPeFormat(MzHeader mzHeader);

    /// <summary>
    /// Reports whether a stream holds a PE file.
    /// </summary>
    /// <param name="stream">The stream to inspect.</param>
    /// <returns><see langword="true"/> if the stream holds a PE file.</returns>
    bool IsPeFormat(Stream stream);
}

/// <summary>
/// The default <see cref="IPeDecoder"/>.
/// </summary>
public sealed class PeFileDecoder : IPeDecoder
{
    /// <inheritdoc />
    public MzHeader DecodeMZ(Stream stream) => MzHeaderReader.Read(stream);

    /// <inheritdoc />
    public bool IsPeFormat(Stream stream) => IsPeFormat(MzHeaderReader.Read(stream));

    /// <inheritdoc />
    public bool IsPeFormat(MzHeader mzHeader) => mzHeader.HasMzSignature;

    /// <inheritdoc />
    public PeHeader DecodePE(Stream stream) => PeHeaderReader.Read(stream);

    /// <inheritdoc />
    public ResourceDirectory? DecodeResourceDirectory(Stream stream, PeHeader peHeader) => ResourceReader.Read(stream, peHeader);
}
