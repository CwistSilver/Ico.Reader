namespace Ico.Reader.PeDecoder.Models;

internal sealed class OptionalHeader
{
    // https://learn.microsoft.com/de-de/windows/win32/debug/pe-format#optional-header-standard-fields-image-only
    public MagicNumber Magic { get; init; }
    public byte MajorLinkerVersion { get; init; }
    public byte MinorLinkerVersion { get; init; }
    public uint SizeOfCode { get; init; }
    public uint SizeOfInitializedData { get; init; }
    public uint SizeOfUninitializedData { get; init; }
    public uint AddressOfEntryPoint { get; init; }
    public uint BaseOfCode { get; init; }
    public uint BaseOfData { get; init; }
    public uint ImageBase { get; init; }
    public uint SectionAlignment { get; init; }
    public uint FileAlignment { get; init; }
    public ushort MajorOperatingSystemVersion { get; init; }
    public ushort MinorOperatingSystemVersion { get; init; }
    public ushort MajorImageVersion { get; init; }
    public ushort MinorImageVersion { get; init; }
    public ushort MajorSubsystemVersion { get; init; }
    public ushort MinorSubsystemVersion { get; init; }
    public uint Win32VersionValue { get; init; }
    public uint SizeOfImage { get; init; }
    public uint SizeOfHeaders { get; init; }
    public uint CheckSum { get; init; }
    public ushort Subsystem { get; init; }
    public ushort DllCharacteristics { get; init; }
    public uint SizeOfStackReserve { get; init; }
    public uint SizeOfStackCommit { get; init; }
    public uint SizeOfHeapReserve { get; init; }
    public uint SizeOfHeapCommit { get; init; }
    public uint LoaderFlags { get; init; }
    public uint NumberOfRvaAndSizes { get; init; }

    public ImageDataDirectory? ExportTable { get; init; }
    public ImageDataDirectory? ImportTable { get; init; }
    public ImageDataDirectory? ResourceTable { get; init; }
    public ImageDataDirectory? ExceptionTable { get; init; }
    public ImageDataDirectory? CertificateTable { get; init; }
    public ImageDataDirectory? BaseRelocationTable { get; init; }
    public ImageDataDirectory? Debug { get; init; }
    public ImageDataDirectory? Architecture { get; init; }
    public ImageDataDirectory? GlobalPtr { get; init; }
    public ImageDataDirectory? TLSTable { get; init; }
    public ImageDataDirectory? LoadConfigTable { get; init; }
    public ImageDataDirectory? BoundImport { get; init; }
    public ImageDataDirectory? IAT { get; init; }
    public ImageDataDirectory? DelayImportDescriptor { get; init; }
    public ImageDataDirectory? CLRRuntimeHeader { get; init; }
    public ImageDataDirectory? Reserved { get; init; }
}
