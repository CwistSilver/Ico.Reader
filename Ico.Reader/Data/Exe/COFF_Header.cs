using System.Runtime.InteropServices;

namespace Ico.Reader.Data.Exe;

/// <summary>
/// See <see href="https://learn.microsoft.com/de-de/windows/win32/debug/pe-format#optional-header-windows-specific-fields-image-only">Optional Header Windows-Specific Fields (Image Only)</see>.
/// </summary>
internal class COFF_Header
{
    internal ulong ImageBase { get; set; }
    internal uint SectionAlignment { get; set; }
    internal uint FileAlignment { get; set; }
    internal ushort MajorOperatingSystemVersion { get; set; }
    internal ushort MinorOperatingSystemVersion { get; set; }
    internal ushort MajorImageVersion { get; set; }
    internal ushort MinorImageVersion { get; set; }
    internal ushort MajorSubsystemVersion { get; set; }
    internal ushort MinorSubsystemVersion { get; set; }
    internal uint Win32VersionValue { get; set; }
    internal uint SizeOfImage { get; set; }
    internal uint SizeOfHeaders { get; set; }
    internal uint CheckSum { get; set; }
    internal ushort Subsystem { get; set; }
    internal ushort DllCharacteristics { get; set; }
    internal ulong SizeOfStackReserve { get; set; }
    internal ulong SizeOfStackCommit { get; set; }
    internal ulong SizeOfHeapReserve { get; set; }
    internal ulong SizeOfHeapCommit { get; set; }
    internal uint LoaderFlags { get; set; }
    internal uint NumberOfRvaAndSizes { get; set; }

    internal static COFF_Header? ReadCOFF(Stream stream, PE_Header header)
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
