namespace Ico.Reader.Data;

/// <summary>
/// The encoding of the image data an entry points at. An ico file may mix both.
/// </summary>
public enum IcoImageFormat
{
    /// <summary>
    /// A device independent bitmap, stored without its file header.
    /// </summary>
    Bmp,

    /// <summary>
    /// A complete PNG file, embedded verbatim.
    /// </summary>
    Png
}
