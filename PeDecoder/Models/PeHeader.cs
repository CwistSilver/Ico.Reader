namespace PeDecoder.Models;

internal sealed class PeHeader
{
    public const uint PeHeaderSize = 24;

    public MachineType Machine { get; init; }
    public ushort NumberOfSections { get; init; }
    public DateTime TimeDateStamp { get; init; }
    public uint PointerToSymbolTable { get; init; }
    public uint NumberOfSymbols { get; init; }
    public ushort SizeOfOptionalHeader { get; init; }
    public Characteristics Characteristics { get; init; }
    public uint HeaderOffset { get; init; }
    public OptionalHeader? Optional { get; init; }
}
