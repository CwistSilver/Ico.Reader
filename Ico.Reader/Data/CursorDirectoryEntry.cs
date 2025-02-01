namespace Ico.Reader.Data;
/// <summary>
/// Represents a directory entry for a cursor (CUR) file.
/// <para>
/// For more information, see <see href="https://en.wikipedia.org/wiki/ICO_(file_format)#Structure_of_image_directory">Structure of image directory</see>.
/// </para>
/// </summary>
public sealed class CursorDirectoryEntry : IIcoDirectoryEntry
{
    public const int ImageType = 2;

    /// <summary>
    /// The X-coordinate of the cursor's hotspot.
    /// The hotspot is the point within the cursor that acts as the click point.
    /// </summary>
    public ushort HotspotX { get; internal set; }

    /// <summary>
    /// The Y-coordinate of the cursor's hotspot.
    /// The hotspot is the point within the cursor that acts as the click point.
    /// </summary>
    public ushort HotspotY { get; internal set; }

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