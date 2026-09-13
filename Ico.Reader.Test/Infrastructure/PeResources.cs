using System.Text;

using Ico.Reader.PeDecoder.Models;

namespace Ico.Reader.Test.Infrastructure;

internal sealed record PeSection(int HeaderOffset, string Name, uint VirtualAddress, uint VirtualSize, uint SizeOfRawData, uint PointerToRawData);

/// <summary>
/// One resource of a PE file, with the file offsets of the structures that describe it.
/// </summary>
/// <param name="Type">The id of the resource type directory entry.</param>
/// <param name="Id">The raw id field of the resource, which has its high bit set when the resource is named.</param>
/// <param name="Language">The id of the language directory entry.</param>
/// <param name="IdEntryOffset">Where the resource's entry sits in its type directory.</param>
/// <param name="LanguageDirectoryOffset">Where the directory listing the resource's languages starts.</param>
/// <param name="DataEntryOffset">Where the data entry pointing at the resource bytes starts.</param>
/// <param name="DataRva">The address of the resource bytes.</param>
/// <param name="Size">The size of the resource bytes.</param>
internal sealed record ResourceLeaf(uint Type, uint Id, uint Language, int IdEntryOffset, int LanguageDirectoryOffset, int DataEntryOffset, uint DataRva, uint Size);

/// <summary>
/// Reads the headers and the resource tree of a PE file straight from its bytes, so tests can patch a file without
/// relying on the decoder they check. Offsets inside the resource tree count from its root, as the Windows loader counts
/// them.
/// </summary>
internal static class PeResources
{
    public const int SectionHeaderSize = 40;
    public const int DataDirectorySize = 8;

    private const int PeHeaderOffsetField = 60;
    private const int SizeOfOptionalHeaderField = 20;
    private const int SectionCountField = 6;
    private const int CoffHeaderSize = 24;
    private const int Pe32NumberOfRvaAndSizesField = 92;
    private const int Pe32PlusNumberOfRvaAndSizesField = 108;
    private const int DirectoryHeaderSize = 16;
    private const int DirectoryEntrySize = 8;
    private const int Pe32DataDirectories = 96;
    private const int Pe32PlusDataDirectories = 112;
    private const ushort Pe32PlusMagic = 0x20B;
    private const int ResourceTableIndex = 2;
    private const uint OffsetMask = 0x7FFFFFFF;

    public static int PeHeaderOffset(byte[] pe) => BitConverter.ToInt32(pe, PeHeaderOffsetField);

    public static int SizeOfOptionalHeaderOffset(byte[] pe) => PeHeaderOffset(pe) + SizeOfOptionalHeaderField;

    public static int OptionalHeaderOffset(byte[] pe) => PeHeaderOffset(pe) + CoffHeaderSize;

    public static bool IsPe32Plus(byte[] pe) => BitConverter.ToUInt16(pe, OptionalHeaderOffset(pe)) == Pe32PlusMagic;

    public static int NumberOfRvaAndSizesOffset(byte[] pe)
        => OptionalHeaderOffset(pe) + (IsPe32Plus(pe) ? Pe32PlusNumberOfRvaAndSizesField : Pe32NumberOfRvaAndSizesField);

    public static int SectionCount(byte[] pe) => BitConverter.ToUInt16(pe, PeHeaderOffset(pe) + SectionCountField);

    public static int DataDirectoryOffset(byte[] pe, int index)
        => OptionalHeaderOffset(pe) + (IsPe32Plus(pe) ? Pe32PlusDataDirectories : Pe32DataDirectories) + (index * DataDirectorySize);

    public static int ResourceTableOffset(byte[] pe) => DataDirectoryOffset(pe, ResourceTableIndex);

    public static int SectionTableOffset(byte[] pe)
        => OptionalHeaderOffset(pe) + BitConverter.ToUInt16(pe, SizeOfOptionalHeaderOffset(pe));

    public static IReadOnlyList<PeSection> Sections(byte[] pe)
    {
        var tableOffset = SectionTableOffset(pe);

        return [.. Enumerable.Range(0, SectionCount(pe)).Select(i =>
        {
            var offset = tableOffset + (i * SectionHeaderSize);
            return new PeSection(
                offset,
                Encoding.ASCII.GetString(pe, offset, 8).TrimEnd('\0'),
                VirtualAddress: BitConverter.ToUInt32(pe, offset + 12),
                VirtualSize: BitConverter.ToUInt32(pe, offset + 8),
                SizeOfRawData: BitConverter.ToUInt32(pe, offset + 16),
                PointerToRawData: BitConverter.ToUInt32(pe, offset + 20));
        })];
    }

    public static PeSection SectionOf(byte[] pe, uint rva)
        => Sections(pe).Single(section => rva >= section.VirtualAddress && rva - section.VirtualAddress < section.SizeOfRawData);

    public static int RvaToOffset(byte[] pe, uint rva)
    {
        var section = SectionOf(pe, rva);
        return (int)(rva - section.VirtualAddress + section.PointerToRawData);
    }

    public static int RootOffset(byte[] pe) => RvaToOffset(pe, BitConverter.ToUInt32(pe, ResourceTableOffset(pe)));

    public static IReadOnlyList<ResourceLeaf> Leaves(byte[] pe)
    {
        var root = RootOffset(pe);
        var leaves = new List<ResourceLeaf>();

        foreach (var (type, _, typeDirectory) in Entries(pe, root, root))
        {
            foreach (var (id, idEntry, languageDirectory) in Entries(pe, root, typeDirectory))
            {
                foreach (var (language, _, dataEntry) in Entries(pe, root, languageDirectory))
                {
                    leaves.Add(new ResourceLeaf(
                        type,
                        id,
                        language,
                        idEntry,
                        languageDirectory,
                        dataEntry,
                        DataRva: BitConverter.ToUInt32(pe, dataEntry),
                        Size: BitConverter.ToUInt32(pe, dataEntry + 4)));
                }
            }
        }

        return leaves;
    }

    public static ResourceLeaf Leaf(byte[] pe, ResourceType type, uint id)
        => Leaves(pe).Single(leaf => leaf.Type == (uint)type && leaf.Id == id);

    public static IEnumerable<ResourceLeaf> LeavesOf(byte[] pe, ResourceType type)
        => Leaves(pe).Where(leaf => leaf.Type == (uint)type);

    public static int DataOffset(byte[] pe, ResourceLeaf leaf) => RvaToOffset(pe, leaf.DataRva);

    public static void WriteUInt16(byte[] pe, int offset, ushort value) => BitConverter.GetBytes(value).CopyTo(pe, offset);

    public static void WriteUInt32(byte[] pe, int offset, uint value) => BitConverter.GetBytes(value).CopyTo(pe, offset);

    private static IEnumerable<(uint Id, int EntryOffset, int TargetOffset)> Entries(byte[] pe, int root, int directory)
    {
        var count = BitConverter.ToUInt16(pe, directory + 12) + BitConverter.ToUInt16(pe, directory + 14);

        for (var i = 0; i < count; i++)
        {
            var entry = directory + DirectoryHeaderSize + (i * DirectoryEntrySize);
            var target = root + (int)(BitConverter.ToUInt32(pe, entry + 4) & OffsetMask);

            yield return (BitConverter.ToUInt32(pe, entry), entry, target);
        }
    }
}
