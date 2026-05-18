using Ico.Reader.Data;
using Ico.Reader.Decoder.ImageDecoder;
using Ico.Reader.Decoder.ImageDecoder.Bmp;

namespace Ico.Reader.Decoder;

/// <inheritdoc cref="IIcoDecoder"/>
public sealed class IcoDecoder : IIcoDecoder
{
    private readonly IDecoder[] _decoders;

    public IcoDecoder(IEnumerable<IDecoder> decoders)
    {
        _decoders = [.. decoders];
    }

    public IcoDecoder()
    {
        _decoders =
        [
            new BmpDecoder(),
            new PngDecoder()
        ];
    }

    public byte[] GetImageData(ReadOnlySpan<byte> imageData, IcoImageFormat format)
    {
        for (var i = 0; i < _decoders.Length; i++)
        {
            if (_decoders[i].SupportedFormat == format)
                return _decoders[i].Decode(imageData);
        }

        throw new NotSupportedException($"The format {format} is not supported.");
    }

    public ImageReference? ReadImageMetadata(ReadOnlySpan<byte> imageData)
    {
        for (var i = 0; i < _decoders.Length; i++)
        {
            if (_decoders[i].IsSupported(imageData))
                return _decoders[i].ReadImageMetadata(imageData);
        }

        return null;
    }

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
