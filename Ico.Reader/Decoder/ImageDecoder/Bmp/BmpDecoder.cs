using System.Runtime.InteropServices;

using Ico.Reader.Creator;
using Ico.Reader.Data;

namespace Ico.Reader.Decoder.ImageDecoder.Bmp;

/// <summary>
/// A decoder that specializes in decoding Ico BMP (Bitmap) image data into a more usable format.
/// </summary>
public sealed class BmpDecoder : IDecoder
{
    private const int InfoHeaderSize = 40;

    /// <summary>
    /// Specifies that this decoder supports the BMP image format.
    /// </summary>
    public IcoImageFormat SupportedFormat => IcoImageFormat.Bmp;

    private readonly Dictionary<int, IIcoBmpDecoder> _decoders = [];
    private readonly IPngCreator _pngCreator;

    /// <summary>
    /// Initializes a new instance of the <see cref="BmpDecoder"/> class with a specific collection of BMP decoders, each supporting different bit depths.
    /// </summary>
    /// <param name="decoders">The collection of decoders for handling various BMP bit depths.</param>
    /// <param name="pngCreator">Creates the PNG wrapper returned for decoded BMP images.</param>
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
    public BmpDecoder() : this(IcoReaderDefaults.CreateBmpDecoders(), IcoReaderDefaults.CreatePngCreator()) { }

    /// <summary>
    /// Decodes BMP image data into an ARGB pixel array based on the image's bit depth.
    /// </summary>
    /// <param name="data">The BMP image data to decode.</param>
    /// <returns>A byte array containing the decoded ARGB pixel data as png.</returns>
    /// <exception cref="NotSupportedException">Thrown if the bit depth of the BMP data is not supported.</exception>
    /// <exception cref="InvalidDataException">Thrown if the header declares dimensions that cannot be decoded.</exception>
    /// <exception cref="EndOfStreamException">Thrown if the data is too short for the pixels the header declares.</exception>
    public byte[] Decode(ReadOnlySpan<byte> data)
    {
        var header = ReadInfoHeader(data);
        if (!_decoders.TryGetValue(header.BitCount, out var decoder))
            throw new NotSupportedException($"The bit count {header.BitCount} is not supported.");

        if (header.Compression != 0)
            throw new NotSupportedException("Compressed BMP images are not supported yet.");

        EnsureDataHoldsThePixels(data, header);

        var argbData = decoder.DecodeIcoBmpToRgba(data, header);

        return _pngCreator.CreatePng(argbData, header);
    }

    /// <summary>
    /// The dimensions come from the file, and every decoder allocates four bytes per declared pixel. They are checked
    /// against the data first, so a few bytes claiming a huge image fail before anything that large is allocated. The
    /// AND mask is not required, because Windows draws an image whose mask is missing.
    /// </summary>
    private static void EnsureDataHoldsThePixels(ReadOnlySpan<byte> data, BmpInfoHeader header)
    {
        var height = header.Height / 2;
        if (header.Width <= 0 || height <= 0)
            throw new InvalidDataException($"The bitmap declares {header.Width}x{height} pixels, which cannot be decoded.");

        if (header.Size < InfoHeaderSize)
            throw new InvalidDataException($"The bitmap header declares {header.Size} bytes, shorter than a BITMAPINFOHEADER.");

        var stride = (((long)header.Width * header.BitCount) + 31) / 32 * 4;
        var pixelDataOffset = header.Size + ((long)header.CalculatePaletteSize() * 4);
        var required = pixelDataOffset + (stride * height);

        if (required > data.Length)
            throw new EndOfStreamException($"The bitmap needs {required} bytes for its header, palette and pixels, but holds {data.Length}.");
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
    public bool IsSupported(ReadOnlySpan<byte> data) => MemoryMarshal.Read<int>(data.Slice(0, 4)) == InfoHeaderSize;

    private static BmpInfoHeader ReadInfoHeader(ReadOnlySpan<byte> src)
    {
        return new BmpInfoHeader
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
