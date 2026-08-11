namespace Ico.Reader.Data;
/// <summary>
/// Represents a directory entry for an icon (ICO) file.
/// This entry contains metadata about an individual icon within an ICO group.
/// <para>
/// For more information, see <see href="https://en.wikipedia.org/wiki/ICO_(file_format)#Structure_of_image_directory">Structure of image directory</see>.
/// </para>
/// </summary>
public sealed class IconDirectoryEntry : IIcoDirectoryEntry
{
    public const int ImageType = 1;

    /// <summary>
    /// Reserved property, should always be set to 0.
    /// </summary>
    public byte Reserved { get; init; }

    /// <summary>
    /// The number of colors in the ico's palette; 0 means the image does not use a palette.
    /// </summary>
    public byte ColorCount { get; init; }

    /// <summary> <inheritdoc/> </summary>
    public byte Width { get; init; }

    /// <summary> <inheritdoc/> </summary>
    public byte Height { get; init; }

    /// <summary> <inheritdoc/> </summary>
    public ushort Planes { get; init; }

    /// <summary> <inheritdoc/> </summary>
    public ushort ColorDepth { get; init; }

    /// <summary> <inheritdoc/> </summary>
    public uint ImageSize { get; init; }

    /// <summary> <inheritdoc/> </summary>
    public uint ImageOffset { get; init; }

    /// <summary> <inheritdoc/> </summary>
    public uint RealImageOffset { get; init; }

    /// <summary>
    /// Returns a copy whose <see cref="RealImageOffset"/> points at the resolved image.
    /// </summary>
    /// <remarks>
    /// Inside a PE, <see cref="ImageOffset"/> holds a resource id rather than a position, so the
    /// real offset is only known once the resource it names has been located.
    /// </remarks>
    internal IconDirectoryEntry WithRealImageOffset(uint realImageOffset) => new()
    {
        Reserved = Reserved,
        ColorCount = ColorCount,
        Width = Width,
        Height = Height,
        Planes = Planes,
        ColorDepth = ColorDepth,
        ImageSize = ImageSize,
        ImageOffset = ImageOffset,
        RealImageOffset = realImageOffset
    };
}
