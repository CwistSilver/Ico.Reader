namespace Ico.Reader.Test.Infrastructure;

/// <summary>Public because it appears in the signatures of public theory methods.</summary>
public readonly record struct CursorImage(int Width, int Height, int BitCount, int HotspotX, int HotspotY);

/// <summary>
/// The hand-authored CUR fixtures and the metadata each one is expected to expose. The hotspots are
/// deliberately asymmetric so an axis swap cannot pass. See CURSOR-FIXTURES.md.
/// </summary>
internal static class CurFixtures
{
    private static readonly (string File, CursorImage Image)[] _singleImage =
    [
        ("cursor_32_1bpp.cur", new(32, 32, 1, 0, 0)),
        ("cursor_32_4bpp.cur", new(32, 32, 4, 5, 5)),
        ("cursor_32_8bpp.cur", new(32, 32, 8, 7, 23)),
        ("cursor_32_24bpp.cur", new(32, 32, 24, 31, 31)),
        ("cursor_32_32bpp.cur", new(32, 32, 32, 10, 20)),
        ("cursor_16_8bpp.cur", new(16, 16, 8, 1, 2)),
        ("cursor_48_32bpp.cur", new(48, 48, 32, 24, 0)),
        ("cursor_256_32bpp.cur", new(256, 256, 32, 128, 200)),
        ("cursor_32_8bpp_masked.cur", new(32, 32, 8, 3, 29)),
    ];

    private static readonly (string File, CursorImage[] Images)[] _multiImage =
    [
        ("cursor_multi.cur",
        [
            new(16, 16, 32, 4, 6),
            new(32, 32, 32, 8, 12),
            new(48, 48, 32, 12, 18),
        ]),
        ("cursor_multi_mixed.cur",
        [
            new(16, 16, 4, 4, 6),
            new(32, 32, 8, 8, 12),
            new(48, 48, 32, 12, 18),
        ]),
    ];

    /// <summary>The 256px cursor, which the editor stored as an embedded PNG rather than a BMP.</summary>
    public const string PngEmbedded = "cursor_256_32bpp.cur";

    /// <summary>The cursor whose AND mask hides the corners.</summary>
    public const string Masked = "cursor_32_8bpp_masked.cur";

    public static TheoryData<string, CursorImage> SingleImage
    {
        get
        {
            TheoryData<string, CursorImage> data = [];
            foreach (var (file, image) in _singleImage)
                data.Add(file, image);

            return data;
        }
    }

    public static TheoryData<string, CursorImage[]> MultiImage
    {
        get
        {
            TheoryData<string, CursorImage[]> data = [];
            foreach (var (file, images) in _multiImage)
                data.Add(file, images);

            return data;
        }
    }

    /// <summary>Every fixture with the images it holds, single and multi alike.</summary>
    public static TheoryData<string, CursorImage[]> All
    {
        get
        {
            TheoryData<string, CursorImage[]> data = [];
            foreach (var (file, image) in _singleImage)
                data.Add(file, [image]);

            foreach (var (file, images) in _multiImage)
                data.Add(file, images);

            return data;
        }
    }

    /// <summary>Every fixture by name, for assertions that do not need the per-image metadata.</summary>
    public static TheoryData<string> AllFiles
    {
        get
        {
            TheoryData<string> data = [];
            foreach (var (file, _) in _singleImage)
                data.Add(file);

            foreach (var (file, _) in _multiImage)
                data.Add(file);

            return data;
        }
    }
}
