namespace Ico.Reader.Data;

/// <summary>
/// Represents the type of an ico file.
/// </summary>
public enum IcoType
{
    /// <summary>
    /// An icon, as found in an .ico file or an RT_GROUP_ICON resource.
    /// </summary>
    Icon,

    /// <summary>
    /// A cursor, which additionally carries a hotspot.
    /// </summary>
    Cursor
}
