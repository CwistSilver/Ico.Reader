using System.Runtime.InteropServices;

using PeDecoder.Models;
using PeDecoder.Utils;

namespace PeDecoder.Reading;

/// <summary>
/// Reads the optional header and the data directories that follow it, whose offsets depend on
/// whether the image is PE32 or PE32+.
/// </summary>
internal static class OptionalHeaderReader
{
    public static OptionalHeader? Read(Stream stream, PeHeader header)
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
            optionalHeader.ExportTable = ReadDataDirectory(optionalHeaderSpan, 96);
            optionalHeader.ImportTable = ReadDataDirectory(optionalHeaderSpan, 104);
            optionalHeader.ResourceTable = ReadDataDirectory(optionalHeaderSpan, 112);
            optionalHeader.ExceptionTable = ReadDataDirectory(optionalHeaderSpan, 120);
            optionalHeader.CertificateTable = ReadDataDirectory(optionalHeaderSpan, 128);
            optionalHeader.BaseRelocationTable = ReadDataDirectory(optionalHeaderSpan, 136);
            optionalHeader.Debug = ReadDataDirectory(optionalHeaderSpan, 144);
            optionalHeader.Architecture = ReadDataDirectory(optionalHeaderSpan, 152);
            optionalHeader.GlobalPtr = ReadDataDirectory(optionalHeaderSpan, 160);
            optionalHeader.TLSTable = ReadDataDirectory(optionalHeaderSpan, 168);
            optionalHeader.LoadConfigTable = ReadDataDirectory(optionalHeaderSpan, 176);
            optionalHeader.BoundImport = ReadDataDirectory(optionalHeaderSpan, 184);
            optionalHeader.IAT = ReadDataDirectory(optionalHeaderSpan, 192);
            optionalHeader.DelayImportDescriptor = ReadDataDirectory(optionalHeaderSpan, 200);
            optionalHeader.CLRRuntimeHeader = ReadDataDirectory(optionalHeaderSpan, 208);
            optionalHeader.Reserved = ReadDataDirectory(optionalHeaderSpan, 216);
        }
        else
        {
            optionalHeader.ExportTable = ReadDataDirectory(optionalHeaderSpan, 112);
            optionalHeader.ImportTable = ReadDataDirectory(optionalHeaderSpan, 120);
            optionalHeader.ResourceTable = ReadDataDirectory(optionalHeaderSpan, 128);
            optionalHeader.ExceptionTable = ReadDataDirectory(optionalHeaderSpan, 136);
            optionalHeader.CertificateTable = ReadDataDirectory(optionalHeaderSpan, 144);
            optionalHeader.BaseRelocationTable = ReadDataDirectory(optionalHeaderSpan, 152);
            optionalHeader.Debug = ReadDataDirectory(optionalHeaderSpan, 160);
            optionalHeader.Architecture = ReadDataDirectory(optionalHeaderSpan, 168);
            optionalHeader.GlobalPtr = ReadDataDirectory(optionalHeaderSpan, 176);
            optionalHeader.TLSTable = ReadDataDirectory(optionalHeaderSpan, 184);
            optionalHeader.LoadConfigTable = ReadDataDirectory(optionalHeaderSpan, 192);
            optionalHeader.BoundImport = ReadDataDirectory(optionalHeaderSpan, 200);
            optionalHeader.IAT = ReadDataDirectory(optionalHeaderSpan, 208);
            optionalHeader.DelayImportDescriptor = ReadDataDirectory(optionalHeaderSpan, 216);
            optionalHeader.CLRRuntimeHeader = ReadDataDirectory(optionalHeaderSpan, 224);
            optionalHeader.Reserved = ReadDataDirectory(optionalHeaderSpan, 232);
        }

        if (optionalHeader.Reserved.VirtualAddress != 0 || optionalHeader.Reserved.Size != 0)
            throw new NotImplementedException("Reserved is not 0!");
    }

    private static ImageDataDirectory ReadDataDirectory(ReadOnlySpan<byte> optionalHeaderSpan, int offset) => new()
    {
        VirtualAddress = MemoryMarshal.Read<uint>(optionalHeaderSpan.Slice(offset, 4)),
        Size = MemoryMarshal.Read<uint>(optionalHeaderSpan.Slice(offset + 4, 4))
    };
}
