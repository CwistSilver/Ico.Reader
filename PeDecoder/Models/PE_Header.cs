using System.Runtime.InteropServices;

namespace PeDecoder.Models;
public class PE_Header
{
    public const uint PeHeaderSize = 24;

    public MachineType Machine { get; set; }
    public ushort NumberOfSections { get; set; }
    public DateTime TimeDateStamp { get; set; }
    public uint PointerToSymbolTable { get; set; }
    public uint NumberOfSymbols { get; set; }
    public ushort SizeOfOptionalHeader { get; set; }
    public Characteristics Characteristics { get; set; }
    public uint HeaderOffset { get; set; }
    public OptionalHeader? Optional { get; set; }

    public static PE_Header ReadFromStream(Stream stream)
    {
        var header = ReadHeader(stream);
        header.Optional = OptionalHeader.ReadFromStream(stream, header);

        return header;
    }

    private static uint GetHeaderOffset(Stream stream)
    {
        stream.Position = 60;

        Span<byte> peHeaderOffset = stackalloc byte[4];
        stream.Read(peHeaderOffset);

        return MemoryMarshal.Read<uint>(peHeaderOffset);
    }

    private static PE_Header ReadHeader(Stream stream)
    {
        var headerOffset = GetHeaderOffset(stream);

        stream.Position = headerOffset;

        Span<byte> data = stackalloc byte[(int)PeHeaderSize];
        stream.Read(data);

        ReadOnlySpan<byte> readOnlyHeaderData = data;

        var header = new PE_Header
        {
            Machine = (MachineType)MemoryMarshal.Read<ushort>(readOnlyHeaderData.Slice(4, 2)),
            NumberOfSections = MemoryMarshal.Read<ushort>(readOnlyHeaderData.Slice(6, 2)),
            TimeDateStamp = new DateTime(1970, 1, 1).AddSeconds(MemoryMarshal.Read<uint>(readOnlyHeaderData.Slice(8, 4))),
            PointerToSymbolTable = MemoryMarshal.Read<uint>(readOnlyHeaderData.Slice(12, 4)),
            NumberOfSymbols = MemoryMarshal.Read<uint>(readOnlyHeaderData.Slice(16, 4)),
            SizeOfOptionalHeader = MemoryMarshal.Read<ushort>(readOnlyHeaderData.Slice(20, 2)),
            Characteristics = (Characteristics)MemoryMarshal.Read<ushort>(readOnlyHeaderData.Slice(22, 2)),
            HeaderOffset = headerOffset
        };

        return header;
    }
}
