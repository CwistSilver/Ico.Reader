using System.Runtime.InteropServices;

using PeDecoder.Utils;

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

    public static OptionalHeader? ReadFromStream(Stream stream, PeHeader header)
    {
        if (header.SizeOfOptionalHeader == 0)
            return null;

        stream.Position = header.HeaderOffset + PeHeader.PeHeaderSize;

        return PooledStreamReader.Read(stream, header.SizeOfOptionalHeader, data =>
        {
            var optionalHeader = new OptionalHeader
            {
                Magic = (MagicNumber)MemoryMarshal.Read<ushort>(data.Slice(0, 2)),
                MajorLinkerVersion = data[2],
                MinorLinkerVersion = data[3],
                SizeOfCode = MemoryMarshal.Read<uint>(data.Slice(4, 4)),
                SizeOfInitializedData = MemoryMarshal.Read<uint>(data.Slice(8, 4)),
                SizeOfUninitializedData = MemoryMarshal.Read<uint>(data.Slice(12, 4)),
                AddressOfEntryPoint = MemoryMarshal.Read<uint>(data.Slice(16, 4)),
                BaseOfCode = MemoryMarshal.Read<uint>(data.Slice(20, 4))
            };

            if (optionalHeader.Magic == MagicNumber.PE32Plus)
                optionalHeader.BaseOfData = MemoryMarshal.Read<uint>(data.Slice(24, 4));

            AddImageDataDirectory(data, optionalHeader);

            return optionalHeader;
        });
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
