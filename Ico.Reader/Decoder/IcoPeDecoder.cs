using System.Runtime.InteropServices;

using Ico.Reader.Data;
using Ico.Reader.Reading;

using Ico.Reader.PeDecoder;
using Ico.Reader.PeDecoder.Models;

namespace Ico.Reader.Decoder;

/// <inheritdoc cref="IIcoPeDecoder"/>
internal sealed class IcoPeDecoder : IIcoPeDecoder
{
    private readonly IPeDecoder _peDecoder;

    public IcoPeDecoder(IPeDecoder peDecoder)
    {
        _peDecoder = peDecoder;
    }

    public DecodedIcoResult? GetDecodedIcoResult(Stream stream, IIcoDecoder icoDecoder)
    {
        var header = _peDecoder.DecodeMZ(stream);
        if (!_peDecoder.IsPeFormat(header))
            return null;

        var peHeader = _peDecoder.DecodePE(stream);
        if (peHeader.Optional is null)
            return null;

        var decodedIcoResult = new DecodedIcoResult
        {
            OriginFileType = peHeader.Characteristics.HasFlag(Characteristics.ImageFileDLL) ? IcoOriginFileType.Dll : IcoOriginFileType.Executable
        };

        var readResourceDirectory = _peDecoder.DecodeResourceDirectory(stream, peHeader);
        if (readResourceDirectory is null)
            return decodedIcoResult;

        AddIcoGroups(decodedIcoResult, readResourceDirectory, stream, icoDecoder);
        AddCurGroups(decodedIcoResult, readResourceDirectory, stream, icoDecoder);

        return decodedIcoResult;
    }

    private void AddIcoGroups(DecodedIcoResult decodedIcoResult, ResourceDirectory readResourceDirectory, Stream stream, IIcoDecoder icoDecoder)
    {
        var icoResource = readResourceDirectory.GetResources(ResourceType.RT_ICON.ToString());
        if (icoResource is null)
            return;

        var sections = readResourceDirectory.Sections;
        decodedIcoResult.References.Capacity = icoResource.Length;

        // Resource ids are only unique within their type, so group entries resolve against icons alone.
        var iconsById = new Dictionary<int, ImageReference>(icoResource.Length);

        for (var i = 0; i < icoResource.Length; i++)
        {
            var icoDataEntry = icoResource[i];
            if (!icoDataEntry.TryGetFileOffset(sections, out var fileOffset))
                continue;

            var reference = ImageReferenceReader.FromStream(stream, fileOffset, icoDataEntry.Size, icoDecoder);
            if (reference is null)
                continue;

            var icon = reference with { Id = (int)icoDataEntry.ID };
            decodedIcoResult.References.Add(icon);
            iconsById.TryAdd(icon.Id, icon);
        }

        var groupDirectory = readResourceDirectory.GetDirectory(ResourceType.RT_GROUP_ICON.ToString());
        if (groupDirectory is null)
            return;

        foreach (var group in groupDirectory.Subdirectories)
        {
            if (!TryGetGroupDataOffset(group, sections, out var fileOffset))
                continue;

            var groupHeader = IcoHeaderReader.Read(stream, fileOffset);
            if (groupHeader.ImageType != IconDirectoryEntry.ImageType)
                continue;

            stream.Position = fileOffset + IcoHeaderReader.HeaderSize;
            var parsedEntries = DirectoryEntryParser.ReadResourceEntries<IconDirectoryEntry>(stream, groupHeader);

            // A group entry names a resource id. Entries naming a resource this file does not carry
            // are dropped, which is why the resolved entries are collected rather than removed in
            // place: removing from the list being indexed would skip whatever shifted down into the
            // vacated slot.
            var directoryEntries = new List<IconDirectoryEntry>(parsedEntries.Length);
            foreach (var entry in parsedEntries)
            {
                if (!iconsById.TryGetValue((int)entry.ImageOffset, out var reference))
                    continue;

                directoryEntries.Add(entry with { RealImageOffset = reference.Offset });
            }

            if (directoryEntries.Count == 0)
                continue;

            decodedIcoResult.IcoGroups.Add(new IconGroup
            {
                Name = group.Name,
                Header = groupHeader,
                DirectoryEntries = [.. directoryEntries]
            });
        }
    }

    private void AddCurGroups(DecodedIcoResult decodedIcoResult, ResourceDirectory readResourceDirectory, Stream stream, IIcoDecoder icoDecoder)
    {
        var curResource = readResourceDirectory.GetResources(ResourceType.RT_CURSOR.ToString());
        if (curResource is null)
            return;

        var sections = readResourceDirectory.Sections;
        decodedIcoResult.References.Capacity += curResource.Length;

        var cursorsById = new Dictionary<int, ImageReference>(curResource.Length);

        // The resource count comes from the file, so allocating per iteration would let a crafted
        // input exhaust the stack.
        Span<byte> hotspotData = stackalloc byte[4];

        for (var i = 0; i < curResource.Length; i++)
        {
            var curDataEntry = curResource[i];
            if (!curDataEntry.TryGetFileOffset(sections, out var fileOffset))
                continue;

            stream.Position = fileOffset;
            stream.ReadExactly(hotspotData);

            var hotspotX = MemoryMarshal.Read<ushort>(hotspotData.Slice(0, 2));
            var hotspotY = MemoryMarshal.Read<ushort>(hotspotData.Slice(2, 2));
            var imageReferenceOffset = fileOffset + 4;

            var reference = ImageReferenceReader.FromStream(stream, imageReferenceOffset, curDataEntry.Size, icoDecoder);
            if (reference is null)
                continue;

            var cursor = reference with { Id = (int)curDataEntry.ID, IcoType = IcoType.Cursor, HotspotX = hotspotX, HotspotY = hotspotY };
            decodedIcoResult.References.Add(cursor);
            cursorsById.TryAdd(cursor.Id, cursor);
        }

        var groupDirectory = readResourceDirectory.GetDirectory(ResourceType.RT_GROUP_CURSOR.ToString());
        if (groupDirectory is null)
            return;

        decodedIcoResult.IcoGroups.Capacity += groupDirectory.Subdirectories.Count;

        foreach (var group in groupDirectory.Subdirectories)
        {
            if (!TryGetGroupDataOffset(group, sections, out var fileOffset))
                continue;

            var groupHeader = IcoHeaderReader.Read(stream, fileOffset);
            if (groupHeader.ImageType != CursorDirectoryEntry.ImageType)
                continue;

            stream.Position = fileOffset + IcoHeaderReader.HeaderSize;
            var parsedEntries = DirectoryEntryParser.ReadResourceEntries<CursorDirectoryEntry>(stream, groupHeader);

            var directoryEntries = new List<CursorDirectoryEntry>(parsedEntries.Length);
            foreach (var entry in parsedEntries)
            {
                if (!cursorsById.TryGetValue((int)entry.ImageOffset, out var reference))
                    continue;

                directoryEntries.Add(entry with { RealImageOffset = reference.Offset, HotspotX = reference.HotspotX, HotspotY = reference.HotspotY });
            }

            if (directoryEntries.Count == 0)
                continue;

            decodedIcoResult.IcoGroups.Add(new CursorGroup
            {
                Name = group.Name,
                Header = groupHeader,
                DirectoryEntries = [.. directoryEntries]
            });
        }
    }

    /// <summary>
    /// A group directory holds one data entry per language, and the group is read from the first. A directory without
    /// any holds no group.
    /// </summary>
    private static bool TryGetGroupDataOffset(ResourceDirectory group, IReadOnlyList<SectionHeader> sections, out uint fileOffset)
    {
        fileOffset = 0;
        return group.DataEntries.Count > 0 && group.DataEntries[0].TryGetFileOffset(sections, out fileOffset);
    }

    public bool IsPeFormat(Stream stream) => _peDecoder.IsPeFormat(stream);
}
