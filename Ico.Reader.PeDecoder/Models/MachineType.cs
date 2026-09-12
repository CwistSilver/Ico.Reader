namespace Ico.Reader.PeDecoder.Models;

/// <summary>
/// The machine architecture an image targets, taken from the COFF header.
/// </summary>
public enum MachineType : ushort
{
    /// <summary>
    /// Applicable to any machine type.
    /// </summary>
    IMAGE_FILE_MACHINE_UNKNOWN = 0x0,

    /// <summary>
    /// Alpha AXP, 32 bit.
    /// </summary>
    IMAGE_FILE_MACHINE_ALPHA = 0x184,

    /// <summary>
    /// Alpha 64, also known as AXP 64.
    /// </summary>
    IMAGE_FILE_MACHINE_ALPHA64 = 0x284,

    /// <summary>
    /// Matsushita AM33.
    /// </summary>
    IMAGE_FILE_MACHINE_AM33 = 0x1d3,

    /// <summary>
    /// x64.
    /// </summary>
    IMAGE_FILE_MACHINE_AMD64 = 0x8664,

    /// <summary>
    /// ARM, little endian.
    /// </summary>
    IMAGE_FILE_MACHINE_ARM = 0x1c0,

    /// <summary>
    /// ARM64, little endian.
    /// </summary>
    IMAGE_FILE_MACHINE_ARM64 = 0xaa64,

    /// <summary>
    /// ARM Thumb-2, little endian.
    /// </summary>
    IMAGE_FILE_MACHINE_ARMNT = 0x1c4,

    /// <summary>
    /// AXP 64. Shares its value with <see cref="IMAGE_FILE_MACHINE_ALPHA64"/>.
    /// </summary>
    IMAGE_FILE_MACHINE_AXP64 = 0x284,

    /// <summary>
    /// EFI byte code.
    /// </summary>
    IMAGE_FILE_MACHINE_EBC = 0xebc,

    /// <summary>
    /// Intel 386 or later, and compatible processors.
    /// </summary>
    IMAGE_FILE_MACHINE_I386 = 0x14c,

    /// <summary>
    /// Intel Itanium.
    /// </summary>
    IMAGE_FILE_MACHINE_IA64 = 0x200,

    /// <summary>
    /// LoongArch, 32 bit.
    /// </summary>
    IMAGE_FILE_MACHINE_LOONGARCH32 = 0x6232,

    /// <summary>
    /// LoongArch, 64 bit.
    /// </summary>
    IMAGE_FILE_MACHINE_LOONGARCH64 = 0x6264,

    /// <summary>
    /// Mitsubishi M32R, little endian.
    /// </summary>
    IMAGE_FILE_MACHINE_M32R = 0x9041,

    /// <summary>
    /// MIPS16.
    /// </summary>
    IMAGE_FILE_MACHINE_MIPS16 = 0x266
}
