namespace Ico.Reader.Data;

/// <summary>
/// Represents the header of an ico resource, detailing the ico's format and the number of images it contains.
/// This header is used to identify the structure of an ico file or resource in memory.
/// </summary>
public sealed class IcoHeader
{
    /// <summary>
    /// Reserved; must always be set to 0.
    /// </summary>
    public ushort Reserved { get; set; }

    /// <summary>
    /// Specifies the type of the image; 1 for ico (.ICO) images, 2 for cursor (.CUR) images.
    /// </summary>
    public ushort ImageType { get; set; }

    /// <summary>
    /// The number of images in the ico or cursor file.
    /// </summary>
    public ushort ImageCount { get; set; }
}
