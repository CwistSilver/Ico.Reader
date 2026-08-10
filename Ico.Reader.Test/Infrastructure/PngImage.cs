using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace Ico.Reader.Test.Infrastructure;

/// <summary>
/// A minimal PNG reader used to verify what the library produces. It validates the container
/// (signature, chunk order, per-chunk CRC) and decodes 8-bit truecolour images to RGBA so decoded
/// pixels can be compared against reference data.
/// </summary>
internal sealed class PngImage
{
    private static readonly byte[] _signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public required int Width { get; init; }
    public required int Height { get; init; }
    public required byte BitDepth { get; init; }
    public required byte ColorType { get; init; }
    public required byte InterlaceMethod { get; init; }
    public required IReadOnlyList<string> ChunkTypes { get; init; }

    /// <summary>Decoded pixels, four bytes per pixel in RGBA order, top row first.</summary>
    public required byte[] Rgba { get; init; }

    public static PngImage Parse(byte[] data)
    {
        if (data.Length < _signature.Length || !data.AsSpan(0, _signature.Length).SequenceEqual(_signature))
            throw new InvalidDataException("Data does not start with the PNG signature.");

        var chunkTypes = new List<string>();
        var compressed = new MemoryStream();
        int width = 0, height = 0;
        byte bitDepth = 0, colorType = 0, interlace = 0;
        var sawEnd = false;

        var position = _signature.Length;
        while (position < data.Length)
        {
            if (position + 12 > data.Length)
                throw new InvalidDataException($"Truncated chunk header at offset {position}.");

            var length = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(position, 4));
            if (length < 0 || position + 12 + length > data.Length)
                throw new InvalidDataException($"Chunk at offset {position} declares an out-of-range length of {length}.");

            var type = Encoding.ASCII.GetString(data, position + 4, 4);
            var content = data.AsSpan(position + 8, length);

            var declaredCrc = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(position + 8 + length, 4));
            var actualCrc = Crc32(data.AsSpan(position + 4, 4 + length));
            if (declaredCrc != actualCrc)
                throw new InvalidDataException($"CRC mismatch in chunk '{type}': declared 0x{declaredCrc:X8}, actual 0x{actualCrc:X8}.");

            chunkTypes.Add(type);
            switch (type)
            {
                case "IHDR":
                    width = BinaryPrimitives.ReadInt32BigEndian(content.Slice(0, 4));
                    height = BinaryPrimitives.ReadInt32BigEndian(content.Slice(4, 4));
                    bitDepth = content[8];
                    colorType = content[9];
                    interlace = content[12];
                    break;
                case "IDAT":
                    compressed.Write(content);
                    break;
                case "IEND":
                    sawEnd = true;
                    break;
            }

            position += 12 + length;
        }

        if (chunkTypes.Count == 0 || chunkTypes[0] != "IHDR")
            throw new InvalidDataException("The first chunk must be IHDR.");
        if (!sawEnd || chunkTypes[^1] != "IEND")
            throw new InvalidDataException("The last chunk must be IEND.");

        return new PngImage
        {
            Width = width,
            Height = height,
            BitDepth = bitDepth,
            ColorType = colorType,
            InterlaceMethod = interlace,
            ChunkTypes = chunkTypes,
            Rgba = Decode(compressed.ToArray(), width, height, bitDepth, colorType, interlace)
        };
    }

    private static byte[] Decode(byte[] compressed, int width, int height, byte bitDepth, byte colorType, byte interlace)
    {
        if (bitDepth != 8)
            throw new NotSupportedException($"Only a bit depth of 8 is supported, got {bitDepth}.");
        if (colorType is not (2 or 6))
            throw new NotSupportedException($"Only colour types 2 and 6 are supported, got {colorType}.");
        if (interlace != 0)
            throw new NotSupportedException("Interlaced images are not supported.");

        var bytesPerPixel = colorType == 6 ? 4 : 3;
        var stride = width * bytesPerPixel;

        using var input = new MemoryStream(compressed);
        using var inflater = new ZLibStream(input, CompressionMode.Decompress);
        using var raw = new MemoryStream();
        inflater.CopyTo(raw);

        var scanlines = raw.ToArray();
        var expectedLength = height * (stride + 1);
        if (scanlines.Length != expectedLength)
            throw new InvalidDataException($"Expected {expectedLength} inflated bytes, got {scanlines.Length}.");

        var rgba = new byte[width * height * 4];
        var previous = new byte[stride];
        var current = new byte[stride];

        for (var y = 0; y < height; y++)
        {
            var rowStart = y * (stride + 1);
            var filter = scanlines[rowStart];
            Array.Copy(scanlines, rowStart + 1, current, 0, stride);
            Unfilter(filter, current, previous, bytesPerPixel);

            for (var x = 0; x < width; x++)
            {
                var source = x * bytesPerPixel;
                var target = ((y * width) + x) * 4;
                rgba[target] = current[source];
                rgba[target + 1] = current[source + 1];
                rgba[target + 2] = current[source + 2];
                rgba[target + 3] = colorType == 6 ? current[source + 3] : (byte)255;
            }

            (previous, current) = (current, previous);
        }

        return rgba;
    }

    /// <summary>
    /// Reverses a PNG scanline filter in place.
    /// <para> Reference: <see href="https://www.w3.org/TR/png/#9Filters">PNG Specification, Filtering</see> </para>
    /// </summary>
    private static void Unfilter(byte filter, byte[] current, byte[] previous, int bytesPerPixel)
    {
        for (var i = 0; i < current.Length; i++)
        {
            var left = i >= bytesPerPixel ? current[i - bytesPerPixel] : (byte)0;
            var up = previous[i];
            var upLeft = i >= bytesPerPixel ? previous[i - bytesPerPixel] : (byte)0;

            current[i] = filter switch
            {
                0 => current[i],
                1 => (byte)(current[i] + left),
                2 => (byte)(current[i] + up),
                3 => (byte)(current[i] + ((left + up) / 2)),
                4 => (byte)(current[i] + Paeth(left, up, upLeft)),
                _ => throw new InvalidDataException($"Unknown scanline filter {filter}.")
            };
        }
    }

    private static byte Paeth(byte left, byte up, byte upLeft)
    {
        var estimate = left + up - upLeft;
        var distanceLeft = Math.Abs(estimate - left);
        var distanceUp = Math.Abs(estimate - up);
        var distanceUpLeft = Math.Abs(estimate - upLeft);

        if (distanceLeft <= distanceUp && distanceLeft <= distanceUpLeft)
            return left;

        return distanceUp <= distanceUpLeft ? up : upLeft;
    }

    private static readonly uint[] _crcTable = [.. Enumerable.Range(0, 256).Select(n =>
    {
        var c = (uint)n;
        for (var k = 0; k < 8; k++)
            c = (c & 1) == 1 ? 0xedb88320 ^ (c >> 1) : c >> 1;

        return c;
    })];

    public static uint Crc32(ReadOnlySpan<byte> data)
    {
        var crc = 0xffffffffu;
        foreach (var b in data)
            crc = _crcTable[(crc ^ b) & 0xff] ^ (crc >> 8);

        return crc ^ 0xffffffff;
    }
}
