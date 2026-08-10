namespace Ico.Reader.Test.Infrastructure;

/// <summary>
/// The committed ICO fixtures and the metadata each one is expected to expose.
/// </summary>
internal static class IcoFixtures
{
    private static readonly (string File, int Width, int Height, int BitCount)[] _singleImage =
    [
        ("icon_16_1bpp.ico", 16, 16, 1),
        ("icon_16_4bpp.ico", 16, 16, 4),
        ("icon_16_8bpp.ico", 16, 16, 8),
        ("icon_16_24bpp.ico", 16, 16, 24),
        ("icon_16_32bpp.ico", 16, 16, 32),
        ("icon_32_1bpp.ico", 32, 32, 1),
        ("icon_32_4bpp.ico", 32, 32, 4),
        ("icon_32_8bpp.ico", 32, 32, 8),
        ("icon_32_24bpp.ico", 32, 32, 24),
        ("icon_32_32bpp.ico", 32, 32, 32),
        ("icon_48_1bpp.ico", 48, 48, 1),
        ("icon_48_4bpp.ico", 48, 48, 4),
        ("icon_48_8bpp.ico", 48, 48, 8),
        ("icon_48_24bpp.ico", 48, 48, 24),
        ("icon_48_32bpp.ico", 48, 48, 32),
        ("icon_256_1bpp.ico", 256, 256, 1),
        ("icon_256_4bpp.ico", 256, 256, 4),
        ("icon_256_8bpp.ico", 256, 256, 8),
        ("icon_256_24bpp.ico", 256, 256, 24),
        ("icon_256_32bpp.ico", 256, 256, 32),
        ("icon_32_1bpp_masked.ico", 32, 32, 1),
        ("icon_32_4bpp_masked.ico", 32, 32, 4),
        ("icon_32_8bpp_masked.ico", 32, 32, 8),
        ("icon_32_24bpp_masked.ico", 32, 32, 24),
        ("icon_32_32bpp_masked.ico", 32, 32, 32),
    ];

    private static readonly (string File, int Entries)[] _multiImage =
    [
        ("icon_multi.ico", 3),
        ("icon_multi_mixed.ico", 3),
        ("icon_multi_png_bmp.ico", 3),
    ];

    /// <summary>The masked fixtures, whose AND mask hides the corners outside the icon's circle.</summary>
    private static readonly string[] _masked =
    [
        "icon_32_1bpp_masked.ico",
        "icon_32_4bpp_masked.ico",
        "icon_32_8bpp_masked.ico",
        "icon_32_24bpp_masked.ico",
        "icon_32_32bpp_masked.ico",
    ];

    /// <summary>A 256x256 icon stored as an embedded PNG rather than a BMP.</summary>
    public const string PngEmbedded = "icon_256_png.ico";

    /// <summary>Single-image fixtures as (file, width, height, bit count).</summary>
    public static TheoryData<string, int, int, int> SingleImage
    {
        get
        {
            TheoryData<string, int, int, int> data = [];
            foreach (var (file, width, height, bitCount) in _singleImage)
                data.Add(file, width, height, bitCount);

            return data;
        }
    }

    public static TheoryData<string> Masked => [.. _masked];

    /// <summary>
    /// Fixtures below 32 bits per pixel with an empty AND mask. Their alpha comes purely from that
    /// mask, so every pixel must decode fully opaque.
    /// </summary>
    public static TheoryData<string> UnmaskedWithoutAlphaChannel
    {
        get
        {
            TheoryData<string> data = [];
            foreach (var (file, _, _, bitCount) in _singleImage)
            {
                if (bitCount != 32 && !_masked.Contains(file))
                    data.Add(file);
            }

            return data;
        }
    }

    /// <summary>Multi-image fixtures as (file, entry count).</summary>
    public static TheoryData<string, int> MultiImage
    {
        get
        {
            TheoryData<string, int> data = [];
            foreach (var (file, entries) in _multiImage)
                data.Add(file, entries);

            return data;
        }
    }

    /// <summary>Every fixture, with the number of entries it holds.</summary>
    public static TheoryData<string, int> All
    {
        get
        {
            TheoryData<string, int> data = [];
            foreach (var (file, _, _, _) in _singleImage)
                data.Add(file, 1);

            data.Add(PngEmbedded, 1);
            foreach (var (file, entries) in _multiImage)
                data.Add(file, entries);

            return data;
        }
    }
}
