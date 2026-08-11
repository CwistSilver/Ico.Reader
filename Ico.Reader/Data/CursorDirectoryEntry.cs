namespace Ico.Reader.Data;
/// <summary>
/// Represents a directory entry for a cursor (CUR) file.
/// <para>
/// For more information, see <see href="https://en.wikipedia.org/wiki/ICO_(file_format)#Structure_of_image_directory">Structure of image directory</see>.
/// </para>
/// </summary>
public sealed record CursorDirectoryEntry : IIcoDirectoryEntry
{
    public const int ImageType = 2;

    /// <summary>
    /// The X-coordinate of the cursor's hotspot.
    /// The hotspot is the point within the cursor that acts as the click point.
    /// </summary>
    public ushort HotspotX { get; internal init; }

    /// <summary>
    /// The Y-coordinate of the cursor's hotspot.
    /// The hotspot is the point within the cursor that acts as the click point.
    /// </summary>
    public ushort HotspotY { get; internal init; }

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
    public uint RealImageOffset { get; internal init; }

    /// <summary>
    /// Returns a copy carrying the resolved image offset and the hotspot read from the resource.
    /// </summary>
    /// <remarks>
    /// Inside a PE, <see cref="ImageOffset"/> holds a resource id rather than a position, and the
    /// hotspot is a prefix on the resource data rather than a field of the group directory.
    /// </remarks>
}
