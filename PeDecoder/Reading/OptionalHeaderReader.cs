using System.Runtime.InteropServices;

using PeDecoder.Models;
using PeDecoder.Utils;

namespace PeDecoder.Reading;

/// <summary>
/// Reads the optional header and the data directories that follow it, whose position depends on
/// whether the image is PE32 or PE32+.
/// </summary>
internal static class OptionalHeaderReader
{
    private const int Pe32DataDirectoryOffset = 96;
    private const int Pe32PlusDataDirectoryOffset = 112;
    private const int DataDirectorySize = 8;

    public static OptionalHeader? Read(Stream stream, uint peHeaderOffset, ushort sizeOfOptionalHeader)
    {
        if (sizeOfOptionalHeader == 0)
            return null;

        stream.Position = peHeaderOffset + PeHeader.PeHeaderSize;

        return PooledStreamReader.Read(stream, sizeOfOptionalHeader, data =>
        {
            var magic = (MagicNumber)MemoryMarshal.Read<ushort>(data.Slice(0, 2));

            // PE32 carries an extra BaseOfData field, which shifts everything after it along.
            var directoryBase = magic == MagicNumber.PE32 ? Pe32DataDirectoryOffset : Pe32PlusDataDirectoryOffset;
            var optionalHeader = new OptionalHeader
            {
                Magic = magic,
                MajorLinkerVersion = data[2],
                MinorLinkerVersion = data[3],
                SizeOfCode = MemoryMarshal.Read<uint>(data.Slice(4, 4)),
                SizeOfInitializedData = MemoryMarshal.Read<uint>(data.Slice(8, 4)),
                SizeOfUninitializedData = MemoryMarshal.Read<uint>(data.Slice(12, 4)),
                AddressOfEntryPoint = MemoryMarshal.Read<uint>(data.Slice(16, 4)),
                BaseOfCode = MemoryMarshal.Read<uint>(data.Slice(20, 4)),
                BaseOfData = magic == MagicNumber.PE32Plus ? MemoryMarshal.Read<uint>(data.Slice(24, 4)) : 0,

                ExportTable = ReadDataDirectory(data, directoryBase + (0 * DataDirectorySize)),
                ImportTable = ReadDataDirectory(data, directoryBase + (1 * DataDirectorySize)),
                ResourceTable = ReadDataDirectory(data, directoryBase + (2 * DataDirectorySize)),
                ExceptionTable = ReadDataDirectory(data, directoryBase + (3 * DataDirectorySize)),
                CertificateTable = ReadDataDirectory(data, directoryBase + (4 * DataDirectorySize)),
                BaseRelocationTable = ReadDataDirectory(data, directoryBase + (5 * DataDirectorySize)),
                Debug = ReadDataDirectory(data, directoryBase + (6 * DataDirectorySize)),
                Architecture = ReadDataDirectory(data, directoryBase + (7 * DataDirectorySize)),
                GlobalPtr = ReadDataDirectory(data, directoryBase + (8 * DataDirectorySize)),
                TLSTable = ReadDataDirectory(data, directoryBase + (9 * DataDirectorySize)),
                LoadConfigTable = ReadDataDirectory(data, directoryBase + (10 * DataDirectorySize)),
                BoundImport = ReadDataDirectory(data, directoryBase + (11 * DataDirectorySize)),
                IAT = ReadDataDirectory(data, directoryBase + (12 * DataDirectorySize)),
                DelayImportDescriptor = ReadDataDirectory(data, directoryBase + (13 * DataDirectorySize)),
                CLRRuntimeHeader = ReadDataDirectory(data, directoryBase + (14 * DataDirectorySize)),
                Reserved = ReadDataDirectory(data, directoryBase + (15 * DataDirectorySize))
            };

            if (optionalHeader.Reserved.VirtualAddress != 0 || optionalHeader.Reserved.Size != 0)
                throw new InvalidDataException("The reserved data directory must be empty.");

            return optionalHeader;
        });
    }

    private static ImageDataDirectory ReadDataDirectory(ReadOnlySpan<byte> optionalHeaderSpan, int offset) => new()
    {
        VirtualAddress = MemoryMarshal.Read<uint>(optionalHeaderSpan.Slice(offset, 4)),
        Size = MemoryMarshal.Read<uint>(optionalHeaderSpan.Slice(offset + 4, 4))
    };
}
