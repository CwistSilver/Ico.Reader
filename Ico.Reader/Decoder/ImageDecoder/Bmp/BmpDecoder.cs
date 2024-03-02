using Ico.Reader.Creator;
using Ico.Reader.Data;
using System.Runtime.InteropServices;

namespace Ico.Reader.Decoder.ImageDecoder.Bmp;
/// <summary>
/// A decoder that specializes in decoding Ico BMP (Bitmap) image data into a more usable format.
/// </summary>
public sealed class BmpDecoder : IDecoder
{
    /// <summary>
    /// Specifies that this decoder supports the BMP image format.
    /// </summary>
    public IcoImageFormat SupportedFormat => IcoImageFormat.BMP;

    private readonly Dictionary<int, IIcoBmpDecoder> _decoders = new();
    private readonly IPngCreator _pngCreator;

    /// <summary>
    /// Initializes a new instance of the <see cref="BmpDecoder"/> class with a specific collection of BMP decoders, each supporting different bit depths.
    /// </summary>
    /// <param name="decoders">The collection of decoders for handling various BMP bit depths.</param>
    public BmpDecoder(IEnumerable<IIcoBmpDecoder> decoders, IPngCreator pngCreator)
    {
        _pngCreator = pngCreator;

        foreach (var decoder in decoders)
        {
            if (!_decoders.TryAdd(decoder.BitCountSupported, decoder))
                _decoders[decoder.BitCountSupported] = decoder;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BmpDecoder"/> class with default decoders for common BMP bit depths.
    /// </summary>
    public BmpDecoder()
    {
        _pngCreator = new PngCreator();

        _decoders.Add(1, new IcoBmp1Decoder());
        _decoders.Add(4, new IcoBmp4Decoder());
        _decoders.Add(8, new IcoBmp8Decoder());
        _decoders.Add(24, new IcoBmp24Decoder());
        _decoders.Add(32, new IcoBmp32Decoder());
    }


    /// <summary>
    /// Decodes BMP image data into an ARGB pixel array based on the image's bit depth.
    /// </summary>
    /// <param name="data">The BMP image data to decode.</param>
    /// <returns>A byte array containing the decoded ARGB pixel data as png.</returns>
    /// <exception cref="NotSupportedException">Thrown if the bit depth of the BMP data is not supported.</exception>
    public byte[] Decode(ReadOnlySpan<byte> data)
    {
        var header = ReadInfoHeader(data);

        if (!_decoders.TryGetValue(header.BitCount, out var decoder))
            throw new NotSupportedException($"The bit count {header.BitCount} is not supported.");


        var argbData = decoder.DecodeIcoBmpToRgba(data, header);

        return _pngCreator.CreatePng(argbData, header);
    }

    /// <summary>
    /// Reads and returns metadata from BMP image data.
    /// </summary>
    /// <param name="data">The BMP image data to analyze.</param>
    /// <returns>An <see cref="ImageReference"/> object containing metadata about the BMP image, or null if the data format is not supported.</returns>
    public ImageReference? ReadImageMetadata(ReadOnlySpan<byte> data)
    {
        return new ImageReference
        {
            Size = MemoryMarshal.Read<uint>(data.Slice(0, 4)),
            Width = MemoryMarshal.Read<int>(data.Slice(4, 4)),
            Height = MemoryMarshal.Read<int>(data.Slice(8, 4)) / 2,
            BitCount = MemoryMarshal.Read<ushort>(data.Slice(14, 2)),
            Format = SupportedFormat
        };
    }

    /// <summary>
    /// Determines if the provided BMP image data is supported by this decoder.
    /// </summary>
    /// <param name="data">The BMP image data to check.</param>
    /// <returns>True if the data is in a supported BMP format; otherwise, false.</returns>
    public bool IsSupported(ReadOnlySpan<byte> data) => MemoryMarshal.Read<int>(data.Slice(0, 4)) == 40;

    private static BMP_Info_Header ReadInfoHeader(ReadOnlySpan<byte> src)
    {
        return new BMP_Info_Header
        {
            Size = MemoryMarshal.Read<int>(src.Slice(0, 4)),
            Width = MemoryMarshal.Read<int>(src.Slice(4, 4)),
            Height = MemoryMarshal.Read<int>(src.Slice(8, 4)),
            Planes = MemoryMarshal.Read<ushort>(src.Slice(12, 2)),
            BitCount = MemoryMarshal.Read<ushort>(src.Slice(14, 2)),
            Compression = MemoryMarshal.Read<int>(src.Slice(16, 4)),
            SizeImage = MemoryMarshal.Read<int>(src.Slice(20, 4)),
            XPelsPerMeter = MemoryMarshal.Read<int>(src.Slice(24, 4)),
            YPelsPerMeter = MemoryMarshal.Read<int>(src.Slice(28, 4)),
            ClrUsed = MemoryMarshal.Read<int>(src.Slice(32, 4)),
            ClrImportant = MemoryMarshal.Read<int>(src.Slice(36, 4))
        };
    }
}
