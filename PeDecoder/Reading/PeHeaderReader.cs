using System.Runtime.InteropServices;

using PeDecoder.Models;

namespace PeDecoder.Reading;

/// <summary>
/// Reads the COFF header the MZ stub points at, together with its optional header.
/// </summary>
internal static class PeHeaderReader
{
    /// <summary>The MZ stub stores the offset of the PE signature at this position.</summary>
    private const int PeSignatureOffsetPosition = 60;

    public static PeHeader Read(Stream stream)
    {
        var headerOffset = ReadHeaderOffset(stream);
        stream.Position = headerOffset;

        Span<byte> buffer = stackalloc byte[(int)PeHeader.PeHeaderSize];
        stream.Read(buffer);

        ReadOnlySpan<byte> data = buffer;
        var sizeOfOptionalHeader = MemoryMarshal.Read<ushort>(data.Slice(20, 2));

        // The optional header sits directly after this one, so it can be read before the header it
        // belongs to is constructed.
        var optional = OptionalHeaderReader.Read(stream, headerOffset, sizeOfOptionalHeader);

        return new PeHeader
        {
            Machine = (MachineType)MemoryMarshal.Read<ushort>(data.Slice(4, 2)),
            NumberOfSections = MemoryMarshal.Read<ushort>(data.Slice(6, 2)),
            TimeDateStamp = DateTimeOffset.FromUnixTimeSeconds(MemoryMarshal.Read<uint>(data.Slice(8, 4))).UtcDateTime,
            PointerToSymbolTable = MemoryMarshal.Read<uint>(data.Slice(12, 4)),
            NumberOfSymbols = MemoryMarshal.Read<uint>(data.Slice(16, 4)),
            SizeOfOptionalHeader = sizeOfOptionalHeader,
            Characteristics = (Characteristics)MemoryMarshal.Read<ushort>(data.Slice(22, 2)),
            HeaderOffset = headerOffset,
            Optional = optional
        };
    }

    private static uint ReadHeaderOffset(Stream stream)
    {
        stream.Position = PeSignatureOffsetPosition;

        Span<byte> peHeaderOffset = stackalloc byte[4];
        stream.Read(peHeaderOffset);

        return MemoryMarshal.Read<uint>(peHeaderOffset);
    }
}
