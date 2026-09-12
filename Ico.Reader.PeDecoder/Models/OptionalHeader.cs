namespace Ico.Reader.PeDecoder.Models;

/// <summary>
/// The optional header of an image, holding the standard and Windows specific fields followed by
/// the data directories.
/// </summary>
/// <remarks>
/// See <see href="https://learn.microsoft.com/en-us/windows/win32/debug/pe-format#optional-header-standard-fields-image-only"/>.
/// </remarks>
public sealed class OptionalHeader
{
    /// <summary>
    /// Whether the image is PE32 or PE32+.
    /// </summary>
    public MagicNumber Magic { get; init; }

    /// <summary>
    /// Major version of the linker that produced the image.
    /// </summary>
    public byte MajorLinkerVersion { get; init; }

    /// <summary>
    /// Minor version of the linker that produced the image.
    /// </summary>
    public byte MinorLinkerVersion { get; init; }

    /// <summary>
    /// Combined size of all code sections.
    /// </summary>
    public uint SizeOfCode { get; init; }

    /// <summary>
    /// Combined size of all initialised data sections.
    /// </summary>
    public uint SizeOfInitializedData { get; init; }

    /// <summary>
    /// Combined size of all uninitialised data sections.
    /// </summary>
    public uint SizeOfUninitializedData { get; init; }

    /// <summary>
    /// Entry point address relative to the image base. Zero for DLLs without one.
    /// </summary>
    public uint AddressOfEntryPoint { get; init; }

    /// <summary>
    /// Address of the first code section relative to the image base.
    /// </summary>
    public uint BaseOfCode { get; init; }

    /// <summary>
    /// Address of the first data section relative to the image base. Present on PE32 only.
    /// </summary>
    public uint BaseOfData { get; init; }

    /// <summary>
    /// Preferred address of the first byte of the image once loaded.
    /// </summary>
    public uint ImageBase { get; init; }

    /// <summary>
    /// Alignment in bytes of sections once loaded.
    /// </summary>
    public uint SectionAlignment { get; init; }

    /// <summary>
    /// Alignment in bytes of the raw section data in the file.
    /// </summary>
    public uint FileAlignment { get; init; }

    /// <summary>
    /// Major version of the required operating system.
    /// </summary>
    public ushort MajorOperatingSystemVersion { get; init; }

    /// <summary>
    /// Minor version of the required operating system.
    /// </summary>
    public ushort MinorOperatingSystemVersion { get; init; }

    /// <summary>
    /// Major version of the image.
    /// </summary>
    public ushort MajorImageVersion { get; init; }

    /// <summary>
    /// Minor version of the image.
    /// </summary>
    public ushort MinorImageVersion { get; init; }

    /// <summary>
    /// Major version of the required subsystem.
    /// </summary>
    public ushort MajorSubsystemVersion { get; init; }

    /// <summary>
    /// Minor version of the required subsystem.
    /// </summary>
    public ushort MinorSubsystemVersion { get; init; }

    /// <summary>
    /// Reserved and required to be zero.
    /// </summary>
    public uint Win32VersionValue { get; init; }

    /// <summary>
    /// Size of the image in bytes once loaded, including all headers.
    /// </summary>
    public uint SizeOfImage { get; init; }

    /// <summary>
    /// Combined size of the DOS stub, the headers and the section table, rounded to the file alignment.
    /// </summary>
    public uint SizeOfHeaders { get; init; }

    /// <summary>
    /// Image checksum, verified for drivers and some system files.
    /// </summary>
    public uint CheckSum { get; init; }

    /// <summary>
    /// The subsystem required to run the image, such as console or GUI.
    /// </summary>
    public ushort Subsystem { get; init; }

    /// <summary>
    /// Attributes of a DLL, such as whether it supports address space layout randomisation.
    /// </summary>
    public ushort DllCharacteristics { get; init; }

    /// <summary>
    /// Stack size to reserve.
    /// </summary>
    public uint SizeOfStackReserve { get; init; }

    /// <summary>
    /// Stack size to commit up front.
    /// </summary>
    public uint SizeOfStackCommit { get; init; }

    /// <summary>
    /// Local heap size to reserve.
    /// </summary>
    public uint SizeOfHeapReserve { get; init; }

    /// <summary>
    /// Local heap size to commit up front.
    /// </summary>
    public uint SizeOfHeapCommit { get; init; }

    /// <summary>
    /// Reserved and required to be zero.
    /// </summary>
    public uint LoaderFlags { get; init; }

    /// <summary>
    /// Number of data directory entries that follow.
    /// </summary>
    public uint NumberOfRvaAndSizes { get; init; }

    /// <summary>
    /// The export table. <see langword="null"/> when the image does not carry it.
    /// </summary>
    public ImageDataDirectory? ExportTable { get; init; }

    /// <summary>
    /// The import table. <see langword="null"/> when the image does not carry it.
    /// </summary>
    public ImageDataDirectory? ImportTable { get; init; }

    /// <summary>
    /// The resource table, which holds the tree <see cref="ResourceDirectory"/> describes. <see langword="null"/> when the image does not carry it.
    /// </summary>
    public ImageDataDirectory? ResourceTable { get; init; }

    /// <summary>
    /// The exception table. <see langword="null"/> when the image does not carry it.
    /// </summary>
    public ImageDataDirectory? ExceptionTable { get; init; }

    /// <summary>
    /// The attribute certificate table. <see langword="null"/> when the image does not carry it.
    /// </summary>
    public ImageDataDirectory? CertificateTable { get; init; }

    /// <summary>
    /// The base relocation table. <see langword="null"/> when the image does not carry it.
    /// </summary>
    public ImageDataDirectory? BaseRelocationTable { get; init; }

    /// <summary>
    /// The debug data. <see langword="null"/> when the image does not carry it.
    /// </summary>
    public ImageDataDirectory? Debug { get; init; }

    /// <summary>
    /// Reserved and required to be zero. <see langword="null"/> when the image does not carry it.
    /// </summary>
    public ImageDataDirectory? Architecture { get; init; }

    /// <summary>
    /// The relative virtual address of the value to be stored in the global pointer register. <see langword="null"/> when the image does not carry it.
    /// </summary>
    public ImageDataDirectory? GlobalPtr { get; init; }

    /// <summary>
    /// The thread local storage table. <see langword="null"/> when the image does not carry it.
    /// </summary>
    public ImageDataDirectory? TLSTable { get; init; }

    /// <summary>
    /// The load configuration table. <see langword="null"/> when the image does not carry it.
    /// </summary>
    public ImageDataDirectory? LoadConfigTable { get; init; }

    /// <summary>
    /// The bound import table. <see langword="null"/> when the image does not carry it.
    /// </summary>
    public ImageDataDirectory? BoundImport { get; init; }

    /// <summary>
    /// The import address table. <see langword="null"/> when the image does not carry it.
    /// </summary>
    public ImageDataDirectory? IAT { get; init; }

    /// <summary>
    /// The delay import descriptor. <see langword="null"/> when the image does not carry it.
    /// </summary>
    public ImageDataDirectory? DelayImportDescriptor { get; init; }

    /// <summary>
    /// The CLR runtime header, present on managed images. <see langword="null"/> when the image does not carry it.
    /// </summary>
    public ImageDataDirectory? CLRRuntimeHeader { get; init; }

    /// <summary>
    /// Reserved and required to be zero. <see langword="null"/> when the image does not carry it.
    /// </summary>
    public ImageDataDirectory? Reserved { get; init; }
}
