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
    public byte Reserved { get; internal set; }

    /// <summary>
    /// The number of colors in the ico's palette; 0 means the image does not use a palette.
    /// </summary>
    public byte ColorCount { get; internal set; }

    /// <summary> <inheritdoc/> </summary>
    public byte Width { get; set; }

    /// <summary> <inheritdoc/> </summary>
    public byte Height { get; set; }

    /// <summary> <inheritdoc/> </summary>
    public ushort Planes { get; set; }

    /// <summary> <inheritdoc/> </summary>
    public ushort ColorDepth { get; set; }

    /// <summary> <inheritdoc/> </summary>
    public uint ImageSize { get; set; }

    /// <summary> <inheritdoc/> </summary>
    public uint ImageOffset { get; set; }

    /// <summary> <inheritdoc/> </summary>
    public uint RealImageOffset { get; set; }
}

