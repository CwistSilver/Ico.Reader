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
        var readResourceDirectory = _peDecoder.DecodeResourceDirectory(stream, peHeader);
        if (readResourceDirectory is null)
            return null;

        var decodedIcoResult = new DecodedIcoResult
        {
            OriginFileType = peHeader.Characteristics.HasFlag(Characteristics.ImageFileDLL) ? IcoOriginFileType.Dll : IcoOriginFileType.Executable
        };

        // The section is resolved once here; every entry offset below is derived from it rather
        // than by re-reading the section table.
        var resourceSection = readResourceDirectory.Section
            ?? throw new InvalidDataException("The resource directory does not carry the section it was read from.");

        AddIcoGroups(decodedIcoResult, readResourceDirectory, resourceSection, stream, icoDecoder);
        AddCurGroups(decodedIcoResult, readResourceDirectory, resourceSection, stream, icoDecoder);

        return decodedIcoResult;
    }

    private void AddIcoGroups(DecodedIcoResult decodedIcoResult, ResourceDirectory readResourceDirectory, SectionHeader resourceSection, Stream stream, IIcoDecoder icoDecoder)
    {
        var icoResource = readResourceDirectory.GetResources(ResourceType.RT_ICON.ToString());
        if (icoResource is null)
            return;

        decodedIcoResult.References.Capacity = icoResource.Length;

        for (var i = 0; i < icoResource.Length; i++)
        {
            var icoDataEntry = icoResource[i];
            var fileOffset = icoDataEntry.GetFileOffset(resourceSection);
            var reference = ImageReferenceReader.FromStream(stream, fileOffset, icoDataEntry.Size, icoDecoder);
            if (reference is null)
                return;

            decodedIcoResult.References.Add(reference with { Id = (int)icoDataEntry.ID });
        }

        var icoResourceGroup = readResourceDirectory.GetResources(ResourceType.RT_GROUP_ICON.ToString());
        if (icoResourceGroup is null)
            return;

        var icoResourceGroupDirectory = readResourceDirectory.GetDirectory(ResourceType.RT_GROUP_ICON.ToString());
        if (icoResourceGroupDirectory is null)
            return;

        for (var i = 0; i < icoResourceGroup.Length; i++)
        {
            var fileOffset = icoResourceGroup[i].GetFileOffset(resourceSection);
            var groupHeader = IcoHeaderReader.Read(stream, fileOffset);

            stream.Position = fileOffset + IcoHeaderReader.HeaderSize;
            var parsedEntries = DirectoryEntryParser.ReadResourceEntries<IconDirectoryEntry>(stream, groupHeader);

            // A group entry names a resource id. Entries naming a resource this file does not carry
            // are dropped, which is why the resolved entries are collected rather than removed in
            // place: removing from the list being indexed would skip whatever shifted down into the
            // vacated slot.
            var directoryEntries = new List<IconDirectoryEntry>(parsedEntries.Length);
            foreach (var entry in parsedEntries)
            {
                var reference = decodedIcoResult.References.FirstOrDefault(r => r.Id == entry.ImageOffset);
                if (reference is null)
                    continue;

                directoryEntries.Add(entry with { RealImageOffset = reference.Offset });
            }

            if (directoryEntries.Count == 0)
                continue;

            decodedIcoResult.IcoGroups.Add(new IconGroup
            {
                Name = icoResourceGroupDirectory.Subdirectories[i].Name,
                Header = groupHeader,
                DirectoryEntries = [.. directoryEntries]
            });
        }
    }

    private void AddCurGroups(DecodedIcoResult decodedIcoResult, ResourceDirectory readResourceDirectory, SectionHeader resourceSection, Stream stream, IIcoDecoder icoDecoder)
    {
        var curResource = readResourceDirectory.GetResources(ResourceType.RT_CURSOR.ToString());
        if (curResource is null)
            return;

        decodedIcoResult.References.Capacity += curResource.Length;

        for (var i = 0; i < curResource.Length; i++)
        {
            var curDataEntry = curResource[i];
            var fileOffset = curDataEntry.GetFileOffset(resourceSection);

            stream.Position = fileOffset;
            Span<byte> hotspotData = stackalloc byte[4];
            stream.Read(hotspotData);

            var hotspotX = MemoryMarshal.Read<ushort>(hotspotData.Slice(0, 2));
            var hotspotY = MemoryMarshal.Read<ushort>(hotspotData.Slice(2, 2));
            var imageReferenceOffset = fileOffset + 4;

            var reference = ImageReferenceReader.FromStream(stream, imageReferenceOffset, curDataEntry.Size, icoDecoder);
            if (reference is null)
                return;

            decodedIcoResult.References.Add(reference with { Id = (int)curDataEntry.ID, IcoType = IcoType.Cursor, HotspotX = hotspotX, HotspotY = hotspotY });
        }

        var curResourceGroup = readResourceDirectory.GetResources(ResourceType.RT_GROUP_CURSOR.ToString());
        if (curResourceGroup is null)
            return;

        var curResourceGroupDirectory = readResourceDirectory.GetDirectory(ResourceType.RT_GROUP_CURSOR.ToString());
        if (curResourceGroupDirectory is null)
            return;

        decodedIcoResult.IcoGroups.Capacity += curResourceGroup.Length;

        for (var i = 0; i < curResourceGroup.Length; i++)
        {
            var fileOffset = curResourceGroup[i].GetFileOffset(resourceSection);
            var groupHeader = IcoHeaderReader.Read(stream, fileOffset);

            stream.Position = fileOffset + IcoHeaderReader.HeaderSize;
            var parsedEntries = DirectoryEntryParser.ReadResourceEntries<CursorDirectoryEntry>(stream, groupHeader);

            var directoryEntries = new List<CursorDirectoryEntry>(parsedEntries.Length);
            foreach (var entry in parsedEntries)
            {
                var reference = decodedIcoResult.References.FirstOrDefault(r => r.Id == entry.ImageOffset);
                if (reference is null)
                    continue;

                directoryEntries.Add(entry with { RealImageOffset = reference.Offset, HotspotX = reference.HotspotX, HotspotY = reference.HotspotY });
            }

            if (directoryEntries.Count == 0)
                continue;

            decodedIcoResult.IcoGroups.Add(new CursorGroup
            {
                Name = curResourceGroupDirectory.Subdirectories[i].Name,
                Header = groupHeader,
                DirectoryEntries = [.. directoryEntries]
            });
        }
    }

    public bool IsPeFormat(Stream stream) => _peDecoder.IsPeFormat(stream);
}
