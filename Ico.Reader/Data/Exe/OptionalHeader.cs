using System.Runtime.InteropServices;

namespace Ico.Reader.Data.Exe;

internal class OptionalHeader
{
    // https://learn.microsoft.com/de-de/windows/win32/debug/pe-format#optional-header-standard-fields-image-only
    internal MagicNumber Magic { get; set; }
    internal byte MajorLinkerVersion { get; set; }
    internal byte MinorLinkerVersion { get; set; }
    internal uint SizeOfCode { get; set; }
    internal uint SizeOfInitializedData { get; set; }
    internal uint SizeOfUninitializedData { get; set; }
    internal uint AddressOfEntryPoint { get; set; }
    internal uint BaseOfCode { get; set; }
    internal uint BaseOfData { get; set; }
    internal uint ImageBase { get; set; }
    internal uint SectionAlignment { get; set; }
    internal uint FileAlignment { get; set; }
    internal ushort MajorOperatingSystemVersion { get; set; }
    internal ushort MinorOperatingSystemVersion { get; set; }
    internal ushort MajorImageVersion { get; set; }
    internal ushort MinorImageVersion { get; set; }
    internal ushort MajorSubsystemVersion { get; set; }
    internal ushort MinorSubsystemVersion { get; set; }
    internal uint Win32VersionValue { get; set; }
    internal uint SizeOfImage { get; set; }
    internal uint SizeOfHeaders { get; set; }
    internal uint CheckSum { get; set; }
    internal ushort Subsystem { get; set; }
    internal ushort DllCharacteristics { get; set; }
    internal uint SizeOfStackReserve { get; set; }
    internal uint SizeOfStackCommit { get; set; }
    internal uint SizeOfHeapReserve { get; set; }
    internal uint SizeOfHeapCommit { get; set; }
    internal uint LoaderFlags { get; set; }
    internal uint NumberOfRvaAndSizes { get; set; }


    internal ImageDataDirectory? ExportTable { get; set; }
    internal ImageDataDirectory? ImportTable { get; set; }
    internal ImageDataDirectory? ResourceTable { get; set; }
    internal ImageDataDirectory? ExceptionTable { get; set; }
    internal ImageDataDirectory? CertificateTable { get; set; }
    internal ImageDataDirectory? BaseRelocationTable { get; set; }
    internal ImageDataDirectory? Debug { get; set; }
    internal ImageDataDirectory? Architecture { get; set; }
    internal ImageDataDirectory? GlobalPtr { get; set; }
    internal ImageDataDirectory? TLSTable { get; set; }
    internal ImageDataDirectory? LoadConfigTable { get; set; }
    internal ImageDataDirectory? BoundImport { get; set; }
    internal ImageDataDirectory? IAT { get; set; }
    internal ImageDataDirectory? DelayImportDescriptor { get; set; }
    internal ImageDataDirectory? CLRRuntimeHeader { get; set; }
    internal ImageDataDirectory? Reserved { get; set; }

    internal static OptionalHeader? ReadFromStream(Stream stream, PE_Header header)
    {
        if (header.SizeOfOptionalHeader == 0)
            return null;

        stream.Position = header.HeaderOffset + PE_Header.PeHeaderSize;
        Span<byte> data = stackalloc byte[header.SizeOfOptionalHeader];
        stream.Read(data);

        ReadOnlySpan<byte> readOnlyData = data;

        var optionalHeader = new OptionalHeader();
        optionalHeader.Magic = (MagicNumber)MemoryMarshal.Read<ushort>(readOnlyData.Slice(0, 2));
        optionalHeader.MajorLinkerVersion = readOnlyData[2];
        optionalHeader.MinorLinkerVersion = readOnlyData[3];
        optionalHeader.SizeOfCode = MemoryMarshal.Read<uint>(readOnlyData.Slice(4, 4));
        optionalHeader.SizeOfInitializedData = MemoryMarshal.Read<uint>(readOnlyData.Slice(8, 4));
        optionalHeader.SizeOfUninitializedData = MemoryMarshal.Read<uint>(readOnlyData.Slice(12, 4));
        optionalHeader.AddressOfEntryPoint = MemoryMarshal.Read<uint>(readOnlyData.Slice(16, 4));
        optionalHeader.BaseOfCode = MemoryMarshal.Read<uint>(readOnlyData.Slice(20, 4));

        if (optionalHeader.Magic == MagicNumber.PE32Plus)
            optionalHeader.BaseOfData = MemoryMarshal.Read<uint>(readOnlyData.Slice(24, 4));

        AddImageDataDirectory(readOnlyData, optionalHeader);

        return optionalHeader;
    }

    private static void AddImageDataDirectory(ReadOnlySpan<byte> optionalHeaderSpan, OptionalHeader optionalHeader)
    {
        if (optionalHeader.Magic == MagicNumber.PE32)
        {
            optionalHeader.ExportTable = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 96);
            optionalHeader.ImportTable = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 104);
            optionalHeader.ResourceTable = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 112);
            optionalHeader.ExceptionTable = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 120);
            optionalHeader.CertificateTable = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 128);
            optionalHeader.BaseRelocationTable = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 136);
            optionalHeader.Debug = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 144);
            optionalHeader.Architecture = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 152);
            optionalHeader.GlobalPtr = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 160);
            optionalHeader.TLSTable = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 168);
            optionalHeader.LoadConfigTable = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 176);
            optionalHeader.BoundImport = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 184);
            optionalHeader.IAT = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 192);
            optionalHeader.DelayImportDescriptor = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 200);
            optionalHeader.CLRRuntimeHeader = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 208);
            optionalHeader.Reserved = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 216);
        }
        else
        {
            optionalHeader.ExportTable = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 112);
            optionalHeader.ImportTable = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 120);
            optionalHeader.ResourceTable = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 128);
            optionalHeader.ExceptionTable = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 136);
            optionalHeader.CertificateTable = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 144);
            optionalHeader.BaseRelocationTable = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 152);
            optionalHeader.Debug = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 160);
            optionalHeader.Architecture = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 168);
            optionalHeader.GlobalPtr = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 176);
            optionalHeader.TLSTable = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 184);
            optionalHeader.LoadConfigTable = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 192);
            optionalHeader.BoundImport = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 200);
            optionalHeader.IAT = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 208);
            optionalHeader.DelayImportDescriptor = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 216);
            optionalHeader.CLRRuntimeHeader = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 224);
            optionalHeader.Reserved = ImageDataDirectory.ReadFromSpan(optionalHeaderSpan, 232);
        }

        if (optionalHeader.Reserved.VirtualAddress != 0 || optionalHeader.Reserved.Size != 0)
            throw new NotImplementedException("Reserved is not 0!");
    }
}
