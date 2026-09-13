using System.Runtime.InteropServices;

using Ico.Reader.PeDecoder.Models;

namespace Ico.Reader.PeDecoder.Reading;

/// <summary>
/// Reads the COFF header the MZ stub points at, together with its optional header.
/// </summary>
internal static class PeHeaderReader
{
    /// <summary>The MZ stub stores the offset of the PE signature at this position.</summary>
    private const int PeSignatureOffsetPosition = 60;

    private static readonly byte[] PeSignature = [(byte)'P', (byte)'E', 0, 0];

    /// <exception cref="InvalidDataException">
    /// The DOS header does not point at a PE signature, as with a 16-bit executable, or the optional header is malformed.
    /// </exception>
    public static PeHeader Read(Stream stream)
    {
        var headerOffset = ReadHeaderOffset(stream);
        stream.Position = headerOffset;

        Span<byte> buffer = stackalloc byte[(int)PeHeader.PeHeaderSize];
        stream.ReadExactly(buffer);

        ReadOnlySpan<byte> data = buffer;
        if (!data.Slice(0, PeSignature.Length).SequenceEqual(PeSignature))
            throw new InvalidDataException($"The DOS header points at offset {headerOffset}, which does not hold a PE signature.");

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

    /// <summary>
    /// Reports whether the offset the DOS header names holds the PE signature. Files that merely start with "MZ", such
    /// as 16-bit executables, do not.
    /// </summary>
    public static bool HasPeSignature(Stream stream)
    {
        if (stream.Length < PeSignatureOffsetPosition + sizeof(uint))
            return false;

        var headerOffset = ReadHeaderOffset(stream);
        if (headerOffset > stream.Length - PeSignature.Length)
            return false;

        stream.Position = headerOffset;

        Span<byte> signature = stackalloc byte[PeSignature.Length];
        stream.ReadExactly(signature);

        return ((ReadOnlySpan<byte>)signature).SequenceEqual(PeSignature);
    }

    private static uint ReadHeaderOffset(Stream stream)
    {
        stream.Position = PeSignatureOffsetPosition;

        Span<byte> peHeaderOffset = stackalloc byte[4];
        stream.ReadExactly(peHeaderOffset);

        return MemoryMarshal.Read<uint>(peHeaderOffset);
    }
}
