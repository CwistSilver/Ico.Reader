﻿using System.Runtime.InteropServices;
using System.Text;

namespace Ico.Reader.Data.Exe;
// https://learn.microsoft.com/en-gb/windows/win32/debug/pe-format?redirectedfrom=MSDN#section-table-section-headers
internal class SectionHeader
{
    internal const uint SectionSize = 40;

    internal string Name { get; set; }
    internal uint VirtualSize { get; set; }
    internal uint VirtualAddress { get; set; }
    internal uint SizeOfRawData { get; set; }
    internal uint PointerToRawData { get; set; }
    internal uint PointerToRelocations { get; set; }
    internal uint PointerToLinenumbers { get; set; }
    internal ushort NumberOfRelocations { get; set; }
    internal ushort NumberOfLinenumbers { get; set; }
    internal SectionFlag Characteristics { get; set; }

    public override string ToString() => $"{Name}";

    internal static SectionHeader[] ReadFromStream(Stream stream, PE_Header peHeader)
    {
        stream.Position = peHeader.SizeOfOptionalHeader + peHeader.HeaderOffset + PE_Header.PeHeaderSize;

        var sections = new SectionHeader[peHeader.NumberOfSections];

        var sectionsSize = peHeader.NumberOfSections * SectionSize;
        Span<byte> data = stackalloc byte[(int)sectionsSize];
        stream.Read(data);

        ReadOnlySpan<byte> readOnlyData = data;
        Span<char> nameChars = stackalloc char[8];

        for (int i = 0; i < peHeader.NumberOfSections; i++)
        {
            var sectionIndex = i * (int)SectionSize;

            sections[i] = new SectionHeader();

            Encoding.UTF8.GetChars(readOnlyData.Slice(sectionIndex, 8), nameChars);
            sections[i].Name = new string(nameChars).Trim('\0');

            sections[i].VirtualSize = MemoryMarshal.Read<uint>(readOnlyData.Slice(sectionIndex + 8, 4));
            sections[i].VirtualAddress = MemoryMarshal.Read<uint>(readOnlyData.Slice(sectionIndex + 12, 4));
            sections[i].SizeOfRawData = MemoryMarshal.Read<uint>(readOnlyData.Slice(sectionIndex + 16, 4));
            sections[i].PointerToRawData = MemoryMarshal.Read<uint>(readOnlyData.Slice(sectionIndex + 20, 4));
            sections[i].PointerToRelocations = MemoryMarshal.Read<uint>(readOnlyData.Slice(sectionIndex + 24, 4));
            sections[i].PointerToLinenumbers = MemoryMarshal.Read<uint>(readOnlyData.Slice(sectionIndex + 28, 4));
            sections[i].NumberOfRelocations = MemoryMarshal.Read<ushort>(readOnlyData.Slice(sectionIndex + 32, 2));
            sections[i].NumberOfLinenumbers = MemoryMarshal.Read<ushort>(readOnlyData.Slice(sectionIndex + 34, 2));
            sections[i].Characteristics = (SectionFlag)MemoryMarshal.Read<uint>(readOnlyData.Slice(sectionIndex + 36, 4));
        }

        return sections;
    }

    internal uint GetFileOffset(uint rva)
    {
        if (rva < VirtualAddress || rva >= VirtualAddress + SizeOfRawData)
            throw new ArgumentOutOfRangeException(nameof(rva), "RVA is outside the range of the section");

        return rva - VirtualAddress + PointerToRawData;
    }
}
