namespace PeDecoder.Models;

internal sealed class OptionalHeader
{
    // https://learn.microsoft.com/de-de/windows/win32/debug/pe-format#optional-header-standard-fields-image-only
    public MagicNumber Magic { get; set; }
    public byte MajorLinkerVersion { get; set; }
    public byte MinorLinkerVersion { get; set; }
    public uint SizeOfCode { get; set; }
    public uint SizeOfInitializedData { get; set; }
    public uint SizeOfUninitializedData { get; set; }
    public uint AddressOfEntryPoint { get; set; }
    public uint BaseOfCode { get; set; }
    public uint BaseOfData { get; set; }
    public uint ImageBase { get; set; }
    public uint SectionAlignment { get; set; }
    public uint FileAlignment { get; set; }
    public ushort MajorOperatingSystemVersion { get; set; }
    public ushort MinorOperatingSystemVersion { get; set; }
    public ushort MajorImageVersion { get; set; }
    public ushort MinorImageVersion { get; set; }
    public ushort MajorSubsystemVersion { get; set; }
    public ushort MinorSubsystemVersion { get; set; }
    public uint Win32VersionValue { get; set; }
    public uint SizeOfImage { get; set; }
    public uint SizeOfHeaders { get; set; }
    public uint CheckSum { get; set; }
    public ushort Subsystem { get; set; }
    public ushort DllCharacteristics { get; set; }
    public uint SizeOfStackReserve { get; set; }
    public uint SizeOfStackCommit { get; set; }
    public uint SizeOfHeapReserve { get; set; }
    public uint SizeOfHeapCommit { get; set; }
    public uint LoaderFlags { get; set; }
    public uint NumberOfRvaAndSizes { get; set; }

    public ImageDataDirectory? ExportTable { get; set; }
    public ImageDataDirectory? ImportTable { get; set; }
    public ImageDataDirectory? ResourceTable { get; set; }
    public ImageDataDirectory? ExceptionTable { get; set; }
    public ImageDataDirectory? CertificateTable { get; set; }
    public ImageDataDirectory? BaseRelocationTable { get; set; }
    public ImageDataDirectory? Debug { get; set; }
    public ImageDataDirectory? Architecture { get; set; }
    public ImageDataDirectory? GlobalPtr { get; set; }
    public ImageDataDirectory? TLSTable { get; set; }
    public ImageDataDirectory? LoadConfigTable { get; set; }
    public ImageDataDirectory? BoundImport { get; set; }
    public ImageDataDirectory? IAT { get; set; }
    public ImageDataDirectory? DelayImportDescriptor { get; set; }
    public ImageDataDirectory? CLRRuntimeHeader { get; set; }
    public ImageDataDirectory? Reserved { get; set; }
}
