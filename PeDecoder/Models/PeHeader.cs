namespace PeDecoder.Models;

internal sealed class PeHeader
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
}
