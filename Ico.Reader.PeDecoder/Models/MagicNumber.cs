namespace Ico.Reader.PeDecoder.Models;

/// <summary>
/// Identifies which form of the optional header a PE file carries.
/// </summary>
public enum MagicNumber : ushort
{
    /// <summary>
    /// A 32 bit image.
    /// </summary>
    PE32 = 0x10b,

    /// <summary>
    /// A 64 bit image. Its optional header omits <see cref="OptionalHeader.BaseOfData"/> and widens
    /// several fields.
    /// </summary>
    PE32Plus = 0x20b,
}
