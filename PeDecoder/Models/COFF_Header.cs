using System.Runtime.InteropServices;

namespace PeDecoder.Models;

/// <summary>
/// See <see href="https://learn.microsoft.com/de-de/windows/win32/debug/pe-format#optional-header-windows-specific-fields-image-only">Optional Header Windows-Specific Fields (Image Only)</see>.
/// </summary>
public class COFF_Header
{
    public ulong ImageBase { get; set; }
    public uint SectionAlignment { get; set; }
    public uint FileAlignment { get; set; }
    public ushort MajorOperatingSystemVersion { get; set; }
    public ushort MinorOperatingSystemVersion { get; set; }
    public ushort MajorImageVersion { get; set; }
    public ushort MinorImageVersion { get; set; }
    public ushort MajorSubsystemVersion { get; set; }
    public ushort MinorSubsystemVersion { get; set; }
    public uint Win32VersionValue { get; set; }
    public uint SizeOfImage { get; set; }
    public uint SizeOfHeaders { get; set; }
    public uint CheckSum { get; set; }
    public ushort Subsystem { get; set; }
    public ushort DllCharacteristics { get; set; }
    public ulong SizeOfStackReserve { get; set; }
    public ulong SizeOfStackCommit { get; set; }
    public ulong SizeOfHeapReserve { get; set; }
    public ulong SizeOfHeapCommit { get; set; }
    public uint LoaderFlags { get; set; }
    public uint NumberOfRvaAndSizes { get; set; }

    public static COFF_Header? ReadCOFF(Stream stream, PE_Header header)
    {
        if (header.Optional is null || header.SizeOfOptionalHeader == 0)
            return null;

        stream.Position = header.HeaderOffset + PE_Header.PeHeaderSize;
        Span<byte> data = stackalloc byte[header.SizeOfOptionalHeader];
        stream.Read(data);

        ReadOnlySpan<byte> optionalHeaderSpan = data;

        var coff = new COFF_Header();

        if (header.Optional.Magic == MagicNumber.PE32)
            coff.ImageBase = MemoryMarshal.Read<uint>(optionalHeaderSpan.Slice(28, 4));
        else
            coff.ImageBase = MemoryMarshal.Read<ulong>(optionalHeaderSpan.Slice(24, 8));

        coff.SectionAlignment = MemoryMarshal.Read<uint>(optionalHeaderSpan.Slice(32, 4));
        coff.FileAlignment = MemoryMarshal.Read<uint>(optionalHeaderSpan.Slice(36, 4));
        coff.MajorOperatingSystemVersion = MemoryMarshal.Read<ushort>(optionalHeaderSpan.Slice(40, 2));
        coff.MinorOperatingSystemVersion = MemoryMarshal.Read<ushort>(optionalHeaderSpan.Slice(42, 2));
        coff.MajorImageVersion = MemoryMarshal.Read<ushort>(optionalHeaderSpan.Slice(44, 2));
        coff.MinorImageVersion = MemoryMarshal.Read<ushort>(optionalHeaderSpan.Slice(46, 2));
        coff.MajorSubsystemVersion = MemoryMarshal.Read<ushort>(optionalHeaderSpan.Slice(48, 2));
        coff.MinorSubsystemVersion = MemoryMarshal.Read<ushort>(optionalHeaderSpan.Slice(50, 2));
        coff.Win32VersionValue = MemoryMarshal.Read<uint>(optionalHeaderSpan.Slice(52, 4));
        coff.SizeOfImage = MemoryMarshal.Read<uint>(optionalHeaderSpan.Slice(56, 4));
        coff.SizeOfHeaders = MemoryMarshal.Read<uint>(optionalHeaderSpan.Slice(60, 4));
        coff.CheckSum = MemoryMarshal.Read<uint>(optionalHeaderSpan.Slice(64, 4));

        coff.Subsystem = MemoryMarshal.Read<ushort>(optionalHeaderSpan.Slice(68, 2));
        coff.DllCharacteristics = MemoryMarshal.Read<ushort>(optionalHeaderSpan.Slice(70, 2));

        if (header.Optional.Magic == MagicNumber.PE32)
        {
            coff.SizeOfStackReserve = MemoryMarshal.Read<uint>(optionalHeaderSpan.Slice(72, 4));
            coff.SizeOfStackCommit = MemoryMarshal.Read<uint>(optionalHeaderSpan.Slice(76, 4));
            coff.SizeOfHeapReserve = MemoryMarshal.Read<uint>(optionalHeaderSpan.Slice(80, 4));
            coff.SizeOfHeapCommit = MemoryMarshal.Read<uint>(optionalHeaderSpan.Slice(84, 4));
            coff.LoaderFlags = MemoryMarshal.Read<uint>(optionalHeaderSpan.Slice(88, 4));
            coff.NumberOfRvaAndSizes = MemoryMarshal.Read<uint>(optionalHeaderSpan.Slice(92, 4));
        }
        else
        {
            coff.SizeOfStackReserve = MemoryMarshal.Read<ulong>(optionalHeaderSpan.Slice(72, 8));
            coff.SizeOfStackCommit = MemoryMarshal.Read<ulong>(optionalHeaderSpan.Slice(80, 8));
            coff.SizeOfHeapReserve = MemoryMarshal.Read<ulong>(optionalHeaderSpan.Slice(88, 8));
            coff.SizeOfHeapCommit = MemoryMarshal.Read<ulong>(optionalHeaderSpan.Slice(96, 8));
            coff.LoaderFlags = MemoryMarshal.Read<uint>(optionalHeaderSpan.Slice(104, 4));
            coff.NumberOfRvaAndSizes = MemoryMarshal.Read<uint>(optionalHeaderSpan.Slice(108, 4));
        }

        return coff;
    }
}
