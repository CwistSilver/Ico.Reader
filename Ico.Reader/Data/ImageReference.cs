namespace Ico.Reader.Data;

/// <summary>
/// Contains metadata about an individual image within an ico file.
/// </summary>
public sealed class ImageReference
{
    /// <summary>
    /// The Id of the image within the PE file.
    /// </summary>
    public int Id { get; internal set; }

    /// <summary>
    /// The offset in the file where the image data begins.
    /// </summary>
    public uint Offset { get; internal set; }

    /// <summary>
    /// The size of the image data in bytes.
    /// </summary>
    public uint Size { get; internal set; }

    /// <summary>
    /// The width of the image in pixels.
    /// </summary>
    public int Width { get; internal set; }

    /// <summary>
    /// The height of the image in pixels.
    /// </summary>
    public int Height { get; internal set; }

    /// <summary>
    /// The bit depth of the image, indicating the number of bits used for each color component.
    /// </summary>
    public int BitCount { get; internal set; }

    /// <summary>
    /// The format of the image, specifying how the image data is encoded.
    /// </summary>
    public IcoImageFormat Format { get; internal set; }

    /// <summary>
    /// Specifies the type of the image, indicating whether it represents an icon (ICO) or a cursor (CUR).
    /// </summary>
    public IcoType IcoType { get; internal set; }

    /// <summary>
    /// The X-coordinate of the cursor's hotspot.
    /// This defines the exact point within the cursor image that interacts with the user interface.
    /// <para>
    /// This property is only relevant for cursor (CUR) images.
    /// </para>
    /// </summary>
    public ushort HotspotX { get; internal set; }

    /// <summary>
    /// The Y-coordinate of the cursor's hotspot.
    /// This defines the exact point within the cursor image that interacts with the user interface.
    /// <para>
    /// This property is only relevant for cursor (CUR) images.
    /// </para>
    /// </summary>
    public ushort HotspotY { get; internal set; }

    internal ImageReference() { }
}
