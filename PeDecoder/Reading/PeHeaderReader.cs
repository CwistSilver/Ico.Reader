using System.Runtime.InteropServices;

using PeDecoder.Models;

namespace PeDecoder.Reading;

/// <summary>
/// Reads the COFF header the MZ stub points at, together with its optional header.
/// </summary>
internal static class PeHeaderReader
{
    public static PeHeader Read(Stream stream)
    {
        var header = ReadHeader(stream);
        header.Optional = OptionalHeaderReader.Read(stream, header);

        return header;
    }

    private static uint GetHeaderOffset(Stream stream)
    {
        stream.Position = 60;

        Span<byte> peHeaderOffset = stackalloc byte[4];
        stream.Read(peHeaderOffset);

        return MemoryMarshal.Read<uint>(peHeaderOffset);
    }

    private static PeHeader ReadHeader(Stream stream)
    {
        var headerOffset = GetHeaderOffset(stream);

        stream.Position = headerOffset;

        Span<byte> data = stackalloc byte[(int)PeHeader.PeHeaderSize];
        stream.Read(data);

        ReadOnlySpan<byte> readOnlyHeaderData = data;

        var header = new PeHeader
        {
            Machine = (MachineType)MemoryMarshal.Read<ushort>(readOnlyHeaderData.Slice(4, 2)),
            NumberOfSections = MemoryMarshal.Read<ushort>(readOnlyHeaderData.Slice(6, 2)),
            TimeDateStamp = DateTimeOffset.FromUnixTimeSeconds(MemoryMarshal.Read<uint>(readOnlyHeaderData.Slice(8, 4))).UtcDateTime,
            PointerToSymbolTable = MemoryMarshal.Read<uint>(readOnlyHeaderData.Slice(12, 4)),
            NumberOfSymbols = MemoryMarshal.Read<uint>(readOnlyHeaderData.Slice(16, 4)),
            SizeOfOptionalHeader = MemoryMarshal.Read<ushort>(readOnlyHeaderData.Slice(20, 2)),
            Characteristics = (Characteristics)MemoryMarshal.Read<ushort>(readOnlyHeaderData.Slice(22, 2)),
            HeaderOffset = headerOffset
        };

        return header;
    }
}
