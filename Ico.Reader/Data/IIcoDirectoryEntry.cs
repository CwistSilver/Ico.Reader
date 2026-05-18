namespace Ico.Reader.Data;
/// <summary>
/// Represents a base directory entry containing metadata for an image within an icon or cursor group.
/// This serves as the base class for specific types of entries, such as <see cref="IconDirectoryEntry"/> for ICO files
/// and <see cref="CursorDirectoryEntry"/> for CUR files.
/// <para>
/// For more information, see <see href="https://en.wikipedia.org/wiki/ICO_(file_format)#Structure_of_image_directory">Structure of image directory</see>.
/// </para>
/// </summary>
public interface IIcoDirectoryEntry
{
    /// <summary>
    /// The width of the ico image in pixels; 0 means image width is 256 pixels.
    /// </summary>
    byte Width { get; internal set; }

    /// <summary>
    /// The height of the ico image in pixels; 0 means image height is 256 pixels.
    /// </summary>
    byte Height { get; internal set; }

    /// <summary>
    /// The number of color planes; typically 0 or 1 for icos.
    /// </summary>
    ushort Planes { get; internal set; }

    /// <summary>
    /// The bit depth of the ico image.
    /// </summary>
    ushort ColorDepth { get; internal set; }

    /// <summary>
    /// The size of the ico image data in bytes.
    /// </summary>
    uint ImageSize { get; internal set; }

    /// <summary>
    /// The offset where the ico image data begins in the file.
    /// </summary>
    uint ImageOffset { get; internal set; }

    /// <summary>
    /// The real offset of the image data in the file.
    /// </summary>
    uint RealImageOffset { get; internal set; }
}
