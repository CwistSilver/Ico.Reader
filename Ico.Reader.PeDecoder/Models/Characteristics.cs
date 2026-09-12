namespace Ico.Reader.PeDecoder.Models;

/// <summary>
/// Attributes of the image, taken from the COFF header.
/// </summary>
[Flags]
public enum Characteristics : ushort
{
    /// <summary>
    /// Base relocations have been stripped, so the image must load at its preferred base address.
    /// </summary>
    ImageFileRelocsStripped = 0x0001,

    /// <summary>
    /// The image is valid and can be run. An image without this flag indicates a link error.
    /// </summary>
    ImageFileExecutableImage = 0x0002,

    /// <summary>
    /// COFF line numbers have been stripped. Deprecated.
    /// </summary>
    ImageFileLineNumsStripped = 0x0004,

    /// <summary>
    /// COFF local symbols have been stripped. Deprecated.
    /// </summary>
    ImageFileLocalSymsStripped = 0x0008,

    /// <summary>
    /// Aggressively trim the working set. Obsolete.
    /// </summary>
    ImageFileAgressibeWsTrim = 0x0010,

    /// <summary>
    /// The application can handle addresses beyond 2 GB.
    /// </summary>
    ImageFileLargeAddressAware = 0x0020,

    /// <summary>
    /// Little endian. Deprecated.
    /// </summary>
    ImageFileBytesReversedLo = 0x0080,

    /// <summary>
    /// The machine is based on a 32 bit word architecture.
    /// </summary>
    ImageFile32BitMachine = 0x0100,

    /// <summary>
    /// Debugging information has been removed from the image.
    /// </summary>
    ImageFileDebugStripped = 0x0200,

    /// <summary>
    /// If the image is on removable media, copy it to and run it from the swap file.
    /// </summary>
    ImageFileRemovableRunFromSwap = 0x0400,

    /// <summary>
    /// If the image is on network media, copy it to and run it from the swap file.
    /// </summary>
    ImageFileNetRunFromSwap = 0x0800,

    /// <summary>
    /// The image is a system file, not a user program.
    /// </summary>
    ImageFileSystem = 0x1000,

    /// <summary>
    /// The image is a dynamic link library rather than an executable.
    /// </summary>
    ImageFileDLL = 0x2000,

    /// <summary>
    /// The file should be run only on a uniprocessor machine.
    /// </summary>
    ImageFileUpSystemOnly = 0x4000,

    /// <summary>
    /// Big endian. Deprecated.
    /// </summary>
    ImageFileBytesReversedHi = 0x8000
}
