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
    private const uint MaxDataDirectories = 16;

    /// <exception cref="InvalidDataException">
    /// The magic is neither PE32 nor PE32+, or the header is shorter than the fields that precede the data directories.
    /// </exception>
    public static OptionalHeader? Read(Stream stream, uint peHeaderOffset, ushort sizeOfOptionalHeader)
    {
        if (sizeOfOptionalHeader == 0)
            return null;

        stream.Position = peHeaderOffset + PeHeader.PeHeaderSize;

        return PooledStreamReader.Read(stream, sizeOfOptionalHeader, data =>
        {
            if (data.Length < 2)
                throw new InvalidDataException("The optional header is too short to name its format.");

            var magic = (MagicNumber)ReadUInt16(data, 0);
            if (magic is not (MagicNumber.PE32 or MagicNumber.PE32Plus))
                throw new InvalidDataException($"The optional header magic 0x{(ushort)magic:X} is neither PE32 nor PE32+.");

            // PE32+ has no BaseOfData and widens ImageBase and the stack and heap sizes to 64 bits, which moves the
            // fields after them. Offsets follow the PE format's Windows-specific fields table.
            var isPe32 = magic == MagicNumber.PE32;
            var directoryBase = isPe32 ? Pe32DataDirectoryOffset : Pe32PlusDataDirectoryOffset;
            if (data.Length < directoryBase)
                throw new InvalidDataException($"The {magic} optional header is {data.Length} bytes, shorter than the {directoryBase} bytes of fields before its data directories.");

            var numberOfRvaAndSizes = ReadUInt32(data, isPe32 ? 92 : 108);
            var directoryCount = DataDirectoryCount(data.Length - directoryBase, numberOfRvaAndSizes);

            return new OptionalHeader
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
                NumberOfRvaAndSizes = numberOfRvaAndSizes,

                ExportTable = ReadDataDirectory(data, directoryBase, 0, directoryCount),
                ImportTable = ReadDataDirectory(data, directoryBase, 1, directoryCount),
                ResourceTable = ReadDataDirectory(data, directoryBase, 2, directoryCount),
                ExceptionTable = ReadDataDirectory(data, directoryBase, 3, directoryCount),
                CertificateTable = ReadDataDirectory(data, directoryBase, 4, directoryCount),
                BaseRelocationTable = ReadDataDirectory(data, directoryBase, 5, directoryCount),
                Debug = ReadDataDirectory(data, directoryBase, 6, directoryCount),
                Architecture = ReadDataDirectory(data, directoryBase, 7, directoryCount),
                GlobalPtr = ReadDataDirectory(data, directoryBase, 8, directoryCount),
                TLSTable = ReadDataDirectory(data, directoryBase, 9, directoryCount),
                LoadConfigTable = ReadDataDirectory(data, directoryBase, 10, directoryCount),
                BoundImport = ReadDataDirectory(data, directoryBase, 11, directoryCount),
                IAT = ReadDataDirectory(data, directoryBase, 12, directoryCount),
                DelayImportDescriptor = ReadDataDirectory(data, directoryBase, 13, directoryCount),
                CLRRuntimeHeader = ReadDataDirectory(data, directoryBase, 14, directoryCount),
                Reserved = ReadDataDirectory(data, directoryBase, 15, directoryCount)
            };
        });
    }

    /// <summary>
    /// The Windows loader reads no data directory past <c>NumberOfRvaAndSizes</c>, and none can lie past the end of the
    /// optional header.
    /// </summary>
    private static int DataDirectoryCount(int bytesAfterFixedFields, uint numberOfRvaAndSizes)
        => (int)Math.Min(Math.Min(numberOfRvaAndSizes, MaxDataDirectories), (uint)(bytesAfterFixedFields / DataDirectorySize));

    private static ImageDataDirectory? ReadDataDirectory(ReadOnlySpan<byte> optionalHeaderSpan, int directoryBase, int index, int directoryCount)
    {
        if (index >= directoryCount)
            return null;

        var offset = directoryBase + (index * DataDirectorySize);
        return new ImageDataDirectory
        {
            VirtualAddress = ReadUInt32(optionalHeaderSpan, offset),
            Size = ReadUInt32(optionalHeaderSpan, offset + 4)
        };
    }

    private static ushort ReadUInt16(ReadOnlySpan<byte> data, int offset) => MemoryMarshal.Read<ushort>(data.Slice(offset, 2));

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset) => MemoryMarshal.Read<uint>(data.Slice(offset, 4));

    private static ulong ReadUInt64(ReadOnlySpan<byte> data, int offset) => MemoryMarshal.Read<ulong>(data.Slice(offset, 8));
}
