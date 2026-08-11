namespace Ico.Reader.Data;

/// <summary>
/// Contains metadata about an individual image within an ico file.
/// </summary>
public sealed class ImageReference
{
    /// <summary>
    /// The Id of the image within the PE file.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// The offset in the file where the image data begins.
    /// </summary>
    public uint Offset { get; init; }

    /// <summary>
    /// The size of the image data in bytes.
    /// </summary>
    public uint Size { get; init; }

    /// <summary>
    /// The width of the image in pixels.
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// The height of the image in pixels.
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// The bit depth of the image, indicating the number of bits used for each color component.
    /// </summary>
    public int BitCount { get; init; }

    /// <summary>
    /// The format of the image, specifying how the image data is encoded.
    /// </summary>
    public IcoImageFormat Format { get; init; }

    /// <summary>
    /// Specifies the type of the image, indicating whether it represents an icon (ICO) or a cursor (CUR).
    /// </summary>
    public IcoType IcoType { get; init; }

    /// <summary>
    /// The X-coordinate of the cursor's hotspot.
    /// This defines the exact point within the cursor image that interacts with the user interface.
    /// <para>
    /// This property is only relevant for cursor (CUR) images.
    /// </para>
    /// </summary>
    public ushort HotspotX { get; init; }

    /// <summary>
    /// The Y-coordinate of the cursor's hotspot.
    /// This defines the exact point within the cursor image that interacts with the user interface.
    /// <para>
    /// This property is only relevant for cursor (CUR) images.
    /// </para>
    /// </summary>
    public ushort HotspotY { get; init; }

    internal ImageReference() { }

    private ImageReference(ImageReference source)
    {
        Id = source.Id;
        Offset = source.Offset;
        Size = source.Size;
        Width = source.Width;
        Height = source.Height;
        BitCount = source.BitCount;
        Format = source.Format;
        IcoType = source.IcoType;
        HotspotX = source.HotspotX;
        HotspotY = source.HotspotY;
    }

    /// <summary>Returns a copy carrying the given id.</summary>
    internal ImageReference WithId(int id) => new(this) { Id = id };

    /// <summary>Returns a copy located at the given position in the source.</summary>
    internal ImageReference WithLocation(uint offset, uint size) => new(this) { Offset = offset, Size = size };

    /// <summary>
    /// Returns a copy with the dimensions a directory entry could not express, which it signals by
    /// storing zero.
    /// </summary>
    internal ImageReference WithDimensions(int width, int height, int bitCount)
        => new(this) { Width = width, Height = height, BitCount = bitCount };

    /// <summary>Returns a copy marked as a cursor with the given hotspot.</summary>
    internal ImageReference AsCursor(ushort hotspotX, ushort hotspotY)
        => new(this) { IcoType = IcoType.Cursor, HotspotX = hotspotX, HotspotY = hotspotY };
}
