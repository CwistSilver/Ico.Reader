namespace Ico.Reader.PeDecoder.Models;

/// <summary>
/// The standard resource types a PE file can hold, as named by the Windows resource compiler. The
/// names of the first level directories in <see cref="ResourceDirectory"/> are these values.
/// </summary>
/// <remarks>
/// See <see href="https://learn.microsoft.com/en-us/windows/win32/menurc/resource-types"/>.
/// </remarks>
public enum ResourceType : uint
{
    /// <summary>
    /// A single cursor image.
    /// </summary>
    RT_CURSOR = 1,

    /// <summary>
    /// A bitmap.
    /// </summary>
    RT_BITMAP = 2,

    /// <summary>
    /// A single icon image.
    /// </summary>
    RT_ICON = 3,

    /// <summary>
    /// A menu.
    /// </summary>
    RT_MENU = 4,

    /// <summary>
    /// A dialog box.
    /// </summary>
    RT_DIALOG = 5,

    /// <summary>
    /// A string table entry.
    /// </summary>
    RT_STRING = 6,

    /// <summary>
    /// A font directory.
    /// </summary>
    RT_FONTDIR = 7,

    /// <summary>
    /// A font.
    /// </summary>
    RT_FONT = 8,

    /// <summary>
    /// An accelerator table.
    /// </summary>
    RT_ACCELERATOR = 9,

    /// <summary>
    /// Application defined raw data.
    /// </summary>
    RT_RCDATA = 10,

    /// <summary>
    /// A message table entry.
    /// </summary>
    RT_MESSAGETABLE = 11,

    /// <summary>
    /// A cursor group directory, naming the <see cref="RT_CURSOR"/> entries that make up one cursor.
    /// </summary>
    RT_GROUP_CURSOR = 12,

    /// <summary>
    /// An icon group directory, naming the <see cref="RT_ICON"/> entries that make up one icon.
    /// </summary>
    RT_GROUP_ICON = 14,

    /// <summary>
    /// A version resource.
    /// </summary>
    RT_VERSION = 16,

    /// <summary>
    /// A header file name for a dialog box.
    /// </summary>
    RT_DLGINCLUDE = 17,

    /// <summary>
    /// Plug and Play data.
    /// </summary>
    RT_PLUGPLAY = 19,

    /// <summary>
    /// A virtual device.
    /// </summary>
    RT_VXD = 20,

    /// <summary>
    /// An animated cursor.
    /// </summary>
    RT_ANICURSOR = 21,

    /// <summary>
    /// An animated icon.
    /// </summary>
    RT_ANIICON = 22,

    /// <summary>
    /// An HTML document.
    /// </summary>
    RT_HTML = 23,

    /// <summary>
    /// A side by side assembly manifest.
    /// </summary>
    RT_MANIFEST = 24
}
