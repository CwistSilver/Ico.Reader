using System.Runtime.InteropServices;

using Ico.Reader.PeDecoder.Models;
using Ico.Reader.PeDecoder.Utils;

namespace Ico.Reader.PeDecoder.Reading;

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
            var magic = (MagicNumber)ReadUInt16(data, 0);

            // PE32+ has no BaseOfData and widens ImageBase and the stack and heap sizes to 64 bits, which moves the
            // fields after them. Offsets follow the PE format's Windows-specific fields table.
            var isPe32 = magic == MagicNumber.PE32;
            var directoryBase = isPe32 ? Pe32DataDirectoryOffset : Pe32PlusDataDirectoryOffset;
            var optionalHeader = new OptionalHeader
            {
                Magic = magic,
                MajorLinkerVersion = data[2],
                MinorLinkerVersion = data[3],
                SizeOfCode = ReadUInt32(data, 4),
                SizeOfInitializedData = ReadUInt32(data, 8),
                SizeOfUninitializedData = ReadUInt32(data, 12),
                AddressOfEntryPoint = ReadUInt32(data, 16),
                BaseOfCode = ReadUInt32(data, 20),
                BaseOfData = isPe32 ? ReadUInt32(data, 24) : 0,

                ImageBase = isPe32 ? ReadUInt32(data, 28) : ReadUInt64(data, 24),
                SectionAlignment = ReadUInt32(data, 32),
                FileAlignment = ReadUInt32(data, 36),
                MajorOperatingSystemVersion = ReadUInt16(data, 40),
                MinorOperatingSystemVersion = ReadUInt16(data, 42),
                MajorImageVersion = ReadUInt16(data, 44),
                MinorImageVersion = ReadUInt16(data, 46),
                MajorSubsystemVersion = ReadUInt16(data, 48),
                MinorSubsystemVersion = ReadUInt16(data, 50),
                Win32VersionValue = ReadUInt32(data, 52),
                SizeOfImage = ReadUInt32(data, 56),
                SizeOfHeaders = ReadUInt32(data, 60),
                CheckSum = ReadUInt32(data, 64),
                Subsystem = ReadUInt16(data, 68),
                DllCharacteristics = ReadUInt16(data, 70),
                SizeOfStackReserve = isPe32 ? ReadUInt32(data, 72) : ReadUInt64(data, 72),
                SizeOfStackCommit = isPe32 ? ReadUInt32(data, 76) : ReadUInt64(data, 80),
                SizeOfHeapReserve = isPe32 ? ReadUInt32(data, 80) : ReadUInt64(data, 88),
                SizeOfHeapCommit = isPe32 ? ReadUInt32(data, 84) : ReadUInt64(data, 96),
                LoaderFlags = ReadUInt32(data, isPe32 ? 88 : 104),
                NumberOfRvaAndSizes = ReadUInt32(data, isPe32 ? 92 : 108),

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
        VirtualAddress = ReadUInt32(optionalHeaderSpan, offset),
        Size = ReadUInt32(optionalHeaderSpan, offset + 4)
    };

    private static ushort ReadUInt16(ReadOnlySpan<byte> data, int offset) => MemoryMarshal.Read<ushort>(data.Slice(offset, 2));

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset) => MemoryMarshal.Read<uint>(data.Slice(offset, 4));

    private static ulong ReadUInt64(ReadOnlySpan<byte> data, int offset) => MemoryMarshal.Read<ulong>(data.Slice(offset, 8));
}
