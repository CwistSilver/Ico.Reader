using Ico.Reader.Data;
using Ico.Reader.Decoder.ImageDecoder;
using Ico.Reader.Decoder.ImageDecoder.Bmp;

namespace Ico.Reader.Decoder;

/// <inheritdoc cref="IIcoDecoder"/>
public sealed class IcoDecoder : IIcoDecoder
{
    private readonly IDecoder[] _decoders;

    /// <summary>
    /// Initializes a new instance of the <see cref="IcoDecoder"/> class with a specific set of image
    /// decoders. The first one claiming a format handles it.
    /// </summary>
    /// <param name="decoders">The decoders to dispatch to, one per supported image format.</param>
    public IcoDecoder(IEnumerable<IDecoder> decoders)
    {
        _decoders = [.. decoders];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IcoDecoder"/> class with the decoders the
    /// library ships with.
    /// </summary>
    public IcoDecoder() : this(IcoReaderDefaults.CreateImageDecoders()) { }

    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">No decoder handles <paramref name="format"/>.</exception>
    public byte[] GetImageData(ReadOnlySpan<byte> imageData, IcoImageFormat format)
    {
        for (var i = 0; i < _decoders.Length; i++)
        {
            if (_decoders[i].SupportedFormat == format)
                return _decoders[i].Decode(imageData);
        }

        throw new NotSupportedException($"The format {format} is not supported.");
    }

    /// <inheritdoc/>
    public ImageReference? ReadImageMetadata(ReadOnlySpan<byte> imageData)
    {
        for (var i = 0; i < _decoders.Length; i++)
        {
            if (_decoders[i].IsSupported(imageData))
                return _decoders[i].ReadImageMetadata(imageData);
        }

        return null;
    }

    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">No decoder recognises the image data.</exception>
    public IcoImageFormat ReadFormat(ReadOnlySpan<byte> imageData)
    {
        for (var i = 0; i < _decoders.Length; i++)
        {
            if (_decoders[i].IsSupported(imageData))
                return _decoders[i].SupportedFormat;
        }

        throw new NotSupportedException("The image format is not supported.");
    }
}
