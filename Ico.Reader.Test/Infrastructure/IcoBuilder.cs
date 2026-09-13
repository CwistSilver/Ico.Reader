namespace Ico.Reader.Test.Infrastructure;

internal readonly record struct Rgb(byte R, byte G, byte B);

/// <summary>
/// Builds ICO and CUR byte streams in memory so tests can express exactly the bytes they mean,
/// including shapes no authoring tool would produce.
/// </summary>
internal sealed class IcoBuilder
{
    private readonly List<(byte[] Entry, byte[] Image)> _entries = [];

    public ushort Reserved { get; set; }
    public ushort ImageType { get; set; } = 1;

    public static IcoBuilder Icon() => new() { ImageType = 1 };

    public static IcoBuilder Cursor() => new() { ImageType = 2 };

    public IcoBuilder AddIcon(byte width, byte height, ushort colorDepth, byte[] image, byte reserved = 0, byte colorCount = 0)
    {
        var entry = new byte[16];
        entry[0] = width;
        entry[1] = height;
        entry[2] = colorCount;
        entry[3] = reserved;
        BitConverter.GetBytes((ushort)1).CopyTo(entry, 4);
        BitConverter.GetBytes(colorDepth).CopyTo(entry, 6);

        _entries.Add((entry, image));
        return this;
    }

    public IcoBuilder AddCursor(byte width, byte height, ushort hotspotX, ushort hotspotY, byte[] image)
    {
        var entry = new byte[16];
        entry[0] = width;
        entry[1] = height;
        BitConverter.GetBytes(hotspotX).CopyTo(entry, 4);
        BitConverter.GetBytes(hotspotY).CopyTo(entry, 6);

        _entries.Add((entry, image));
        return this;
    }

    public byte[] Build(ushort? imageCountOverride = null)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(Reserved);
        writer.Write(ImageType);
        writer.Write(imageCountOverride ?? (ushort)_entries.Count);

        var imageOffset = 6 + (_entries.Count * 16);
        foreach (var (entry, image) in _entries)
        {
            BitConverter.GetBytes((uint)image.Length).CopyTo(entry, 8);
            BitConverter.GetBytes((uint)imageOffset).CopyTo(entry, 12);
            writer.Write(entry);
            imageOffset += image.Length;
        }

        foreach (var (_, image) in _entries)
            writer.Write(image);

        writer.Flush();
        return stream.ToArray();
    }
}

/// <summary>
/// Builds the BMP payload of a single ICO entry: info header, palette, bottom-up pixel rows and
/// the 1-bit AND mask.
/// </summary>
internal static class IcoBmpImage
{
    private const int InfoHeaderSize = 40;

    /// <param name="indices">Palette indices addressed as [y, x], with row 0 at the top.</param>
    /// <param name="transparent">AND mask addressed as [y, x]; true hides the pixel.</param>
    /// <remarks>
    /// The palette is written at exactly <paramref name="palette"/>.Length entries and declared as
    /// such in ClrUsed, so a palette shorter than the bit depth allows produces the truncated layout
    /// a real encoder would emit.
    /// </remarks>
    public static byte[] Indexed(int bitCount, Rgb[] palette, byte[,] indices, bool[,]? transparent = null)
    {
        var height = indices.GetLength(0);
        var width = indices.GetLength(1);

        var paletteLength = palette.Length;
        var pixelStride = Stride(width * bitCount);

        var pixels = new byte[pixelStride * height];
        for (var y = 0; y < height; y++)
        {
            var row = (height - 1 - y) * pixelStride;
            for (var x = 0; x < width; x++)
            {
                var index = indices[y, x];
                switch (bitCount)
                {
                    case 1:
                        if (index != 0)
                            pixels[row + (x / 8)] |= (byte)(1 << (7 - (x % 8)));
                        break;
                    case 4:
                        pixels[row + (x / 2)] |= x % 2 == 0 ? (byte)(index << 4) : (byte)(index & 0x0F);
                        break;
                    case 8:
                        pixels[row + x] = index;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(bitCount), bitCount, "Indexed images are 1, 4 or 8 bit.");
                }
            }
        }

        return Compose(width, height, bitCount, paletteLength, palette, pixels, transparent);
    }

    /// <param name="pixels">Colours addressed as [y, x], with row 0 at the top.</param>
    /// <param name="transparent">AND mask addressed as [y, x]; true hides the pixel.</param>
    /// <param name="palette">
    /// An optional palette. A true colour bitmap does not need one, but the format allows it to
    /// declare one, and the pixel data then starts after it.
    /// </param>
    public static byte[] TrueColor24(Rgb[,] pixels, bool[,]? transparent = null, Rgb[]? palette = null)
    {
        var height = pixels.GetLength(0);
        var width = pixels.GetLength(1);
        var stride = Stride(width * 24);

        var data = new byte[stride * height];
        for (var y = 0; y < height; y++)
        {
            var row = (height - 1 - y) * stride;
            for (var x = 0; x < width; x++)
            {
                data[row + (x * 3)] = pixels[y, x].B;
                data[row + (x * 3) + 1] = pixels[y, x].G;
                data[row + (x * 3) + 2] = pixels[y, x].R;
            }
        }

        return Compose(width, height, 24, palette?.Length ?? 0, palette ?? [], data, transparent);
    }

    /// <param name="pixels">Colours addressed as [y, x] with alpha, row 0 at the top.</param>
    /// <param name="palette"><inheritdoc cref="TrueColor24" path="/param[@name='palette']"/></param>
    /// <param name="transparent">AND mask addressed as [y, x]; true hides the pixel.</param>
    public static byte[] TrueColor32((Rgb Color, byte Alpha)[,] pixels, Rgb[]? palette = null, bool[,]? transparent = null)
    {
        var height = pixels.GetLength(0);
        var width = pixels.GetLength(1);
        var stride = width * 4;

        var data = new byte[stride * height];
        for (var y = 0; y < height; y++)
        {
            var row = (height - 1 - y) * stride;
            for (var x = 0; x < width; x++)
            {
                data[row + (x * 4)] = pixels[y, x].Color.B;
                data[row + (x * 4) + 1] = pixels[y, x].Color.G;
                data[row + (x * 4) + 2] = pixels[y, x].Color.R;
                data[row + (x * 4) + 3] = pixels[y, x].Alpha;
            }
        }

        return Compose(width, height, 32, palette?.Length ?? 0, palette ?? [], data, transparent);
    }

    private static byte[] Compose(int width, int height, int bitCount, int paletteLength, Rgb[] palette, byte[] pixels, bool[,]? transparent)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(InfoHeaderSize);
        writer.Write(width);
        writer.Write(height * 2);
        writer.Write((ushort)1);
        writer.Write((ushort)bitCount);
        writer.Write(0);
        writer.Write(pixels.Length);
        writer.Write(0);
        writer.Write(0);
        writer.Write(paletteLength);
        writer.Write(0);

        for (var i = 0; i < paletteLength; i++)
        {
            writer.Write(palette[i].B);
            writer.Write(palette[i].G);
            writer.Write(palette[i].R);
            writer.Write((byte)0);
        }

        writer.Write(pixels);
        writer.Write(BuildMask(width, height, transparent));
        writer.Flush();

        return stream.ToArray();
    }

    private static byte[] BuildMask(int width, int height, bool[,]? transparent)
    {
        var stride = Stride(width);
        var mask = new byte[stride * height];
        if (transparent is null)
            return mask;

        for (var y = 0; y < height; y++)
        {
            var row = (height - 1 - y) * stride;
            for (var x = 0; x < width; x++)
            {
                if (transparent[y, x])
                    mask[row + (x / 8)] |= (byte)(1 << (7 - (x % 8)));
            }
        }

        return mask;
    }

    private static int Stride(int bitsPerRow) => ((bitsPerRow + 31) / 32) * 4;
}
