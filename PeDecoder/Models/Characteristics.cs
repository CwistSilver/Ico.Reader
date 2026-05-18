namespace PeDecoder.Models;

[Flags]
public enum Characteristics : ushort
{
    ImageFileRelocsStripped = 0x0001,
    ImageFileExecutableImage = 0x0002,
    ImageFileLineNumsStripped = 0x0004,
    ImageFileLocalSymsStripped = 0x0008,
    ImageFileAgressibeWsTrim = 0x0010,
    ImageFileLargeAddressAware = 0x0020,
    ImageFileBytesReversedLo = 0x0080,
    ImageFile32BitMachine = 0x0100,
    ImageFileDebugStripped = 0x0200,
    ImageFileRemovableRunFromSwap = 0x0400,
    ImageFileNetRunFromSwap = 0x0800,
    ImageFileSystem = 0x1000,
    ImageFileDLL = 0x2000,
    ImageFileUpSystemOnly = 0x4000,
    ImageFileBytesReversedHi = 0x8000
}
