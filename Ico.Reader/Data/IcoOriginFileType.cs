namespace Ico.Reader.Data;

/// <summary>
/// The kind of file the ico data was read from.
/// </summary>
public enum IcoOriginFileType
{
    /// <summary>
    /// The file is an executable file.
    /// </summary>
    Executable,

    /// <summary>
    /// The file is a DLL file.
    /// </summary>
    Dll,

    /// <summary>
    /// The file is an ico file.
    /// </summary>
    Ico,

    /// <summary>
    /// The file is a cursor file.
    /// </summary>
    Cur
}
