namespace Ico.Reader.Data;
/// <summary>
/// Represents a directory entry for an icon (ICO) file.
/// This entry contains metadata about an individual icon within an ICO group.
/// <para>
/// For more information, see <see href="https://en.wikipedia.org/wiki/ICO_(file_format)#Structure_of_image_directory">Structure of image directory</see>.
/// </para>
/// </summary>
public sealed record IconDirectoryEntry : IIcoDirectoryEntry
{
    /// <summary>
    /// The value an ico header carries in its image type field for icons.
    /// </summary>
    public const int ImageType = 1;

    /// <summary>
    /// Reserved property, should always be set to 0.
    /// </summary>
    public byte Reserved { get; internal init; }

    /// <summary>
    /// The number of colors in the ico's palette; 0 means the image does not use a palette.
    /// </summary>
    public byte ColorCount { get; internal init; }

    /// <summary> <inheritdoc/> </summary>
    public byte Width { get; internal init; }

    /// <summary> <inheritdoc/> </summary>
    public byte Height { get; internal init; }

    /// <summary> <inheritdoc/> </summary>
    public ushort Planes { get; internal init; }

    /// <summary> <inheritdoc/> </summary>
    public ushort ColorDepth { get; internal init; }

    /// <summary> <inheritdoc/> </summary>
    public uint ImageSize { get; internal init; }

    /// <summary> <inheritdoc/> </summary>
    public uint ImageOffset { get; internal init; }

    /// <summary> <inheritdoc/> </summary>
    /// <remarks>
    /// Inside a PE, <see cref="ImageOffset"/> holds a resource id rather than a position, so the
    /// real offset is only known once the resource it names has been located.
    /// </remarks>
    public uint RealImageOffset { get; internal init; }
}
