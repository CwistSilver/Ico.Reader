using Ico.Reader.Data;
using PeDecoder;
using PeDecoder.Models;
using System.Runtime.InteropServices;

namespace Ico.Reader.Decoder;
/// <inheritdoc cref="IIcoPeDecoder"/>
public sealed class IcoPeDecoder : IIcoPeDecoder
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

        AddIcoGroups(decodedIcoResult, readResourceDirectory, peHeader, stream, icoDecoder);
        AddCurGroups(decodedIcoResult, readResourceDirectory, peHeader, stream, icoDecoder);

        return decodedIcoResult;
    }

    private void AddIcoGroups(DecodedIcoResult decodedIcoResult, ResourceDirectory readResourceDirectory, PE_Header peHeader, Stream stream, IIcoDecoder icoDecoder)
    {
        var icoResource = readResourceDirectory.GetResources(ResourceType.RT_ICON.ToString());
        if (icoResource is null)
            return;

        decodedIcoResult.References.Capacity = icoResource.Length;

        for (int i = 0; i < icoResource.Length; i++)
        {
            var icoDataEntry = icoResource[i];
            var fileOffset = icoDataEntry.GetFileOffset(stream, peHeader);
            var reference = ImageReference.FromStream(stream, fileOffset, icoDataEntry.Size, icoDecoder);
            if (reference is null)
                return;

            reference.Id = (int)icoDataEntry.ID;
            decodedIcoResult.References.Add(reference);
        }

        var icoResourceGroup = readResourceDirectory.GetResources(ResourceType.RT_GROUP_ICON.ToString());
        if (icoResourceGroup is null)
            return;

        var icoResourceGroupDirectory = readResourceDirectory.GetDirectory(ResourceType.RT_GROUP_ICON.ToString());
        if (icoResourceGroupDirectory is null)
            return;

        for (int i = 0; i < icoResourceGroup.Length; i++)
        {
            var icoGroup = new IconGroup()
            {
                Name = icoResourceGroupDirectory.Subdirectories[i].Name
            };

            var fileOffset = icoResourceGroup[i].GetFileOffset(stream, peHeader);
            icoGroup.Header = IcoHeader.ReadFromStream(stream, fileOffset);

            stream.Position = fileOffset + 6;
            var directoryEntries = icoGroup.ReadEntriesFromEXEStream(stream, icoGroup.Header).ToList();

            for (int x = 0; x < directoryEntries.Count; x++)
            {
                var reference = decodedIcoResult.References.FirstOrDefault(r => r.Id == directoryEntries[x].ImageOffset);
                if (reference is null)
                    directoryEntries.RemoveAt(x);
                else
                    directoryEntries[x].RealImageOffset = reference.Offset;
            }

            if (directoryEntries.Count == 0)
                continue;

            icoGroup.DirectoryEntries = directoryEntries.ToArray();
            decodedIcoResult.IcoGroups.Add(icoGroup as IIcoGroup);
        }
    }

    private void AddCurGroups(DecodedIcoResult decodedIcoResult, ResourceDirectory readResourceDirectory, PE_Header peHeader, Stream stream, IIcoDecoder icoDecoder)
    {
        var curResource = readResourceDirectory.GetResources(ResourceType.RT_CURSOR.ToString());
        if (curResource is null)
            return;

        decodedIcoResult.References.Capacity += curResource.Length;

        for (int i = 0; i < curResource.Length; i++)
        {
            var curDataEntry = curResource[i];
            var fileOffset = curDataEntry.GetFileOffset(stream, peHeader);

            stream.Position = fileOffset;
            Span<byte> hotspotData = stackalloc byte[4];
            stream.Read(hotspotData);

            var hotspotX = MemoryMarshal.Read<ushort>(hotspotData.Slice(0, 2));
            var hotspotY = MemoryMarshal.Read<ushort>(hotspotData.Slice(2, 2));
            var imageReferenceOffset = fileOffset + 4;

            var reference = ImageReference.FromStream(stream, imageReferenceOffset, curDataEntry.Size, icoDecoder);
            if (reference is null)
                return;

            reference.Id = (int)curDataEntry.ID;
            reference.IcoType = IcoType.Cursor;
            reference.HotspotX = hotspotX;
            reference.HotspotY = hotspotY;

            decodedIcoResult.References.Add(reference);
        }

        var curResourceGroup = readResourceDirectory.GetResources(ResourceType.RT_GROUP_CURSOR.ToString());
        if (curResourceGroup is null)
            return;

        var curResourceGroupDirectory = readResourceDirectory.GetDirectory(ResourceType.RT_GROUP_CURSOR.ToString());
        if (curResourceGroupDirectory is null)
            return;

        decodedIcoResult.IcoGroups.Capacity += curResourceGroup.Length;

        for (int i = 0; i < curResourceGroup.Length; i++)
        {
            var curGroup = new CursorGroup()
            {
                Name = curResourceGroupDirectory.Subdirectories[i].Name
            };

            var fileOffset = curResourceGroup[i].GetFileOffset(stream, peHeader);
            curGroup.Header = IcoHeader.ReadFromStream(stream, fileOffset);
            stream.Position = fileOffset + 6;
            var directoryEntries = curGroup.ReadEntriesFromEXEStream(stream, curGroup.Header).ToList();

            for (int x = 0; x < directoryEntries.Count; x++)
            {
                var reference = decodedIcoResult.References.FirstOrDefault(r => r.Id == directoryEntries[x].ImageOffset);
                if (reference is null)
                    directoryEntries.RemoveAt(x);
                else
                {
                    var directoryEntry = directoryEntries[x];
                    directoryEntry.RealImageOffset = reference.Offset;
                    directoryEntry.HotspotX = reference.HotspotX;
                    directoryEntry.HotspotY = reference.HotspotY;
                }
            }

            if (directoryEntries.Count == 0)
                continue;

            curGroup.DirectoryEntries = directoryEntries.ToArray();
            decodedIcoResult.IcoGroups.Add(curGroup);
        }
    }

    public bool IsPeFormat(Stream stream) => _peDecoder.IsPeFormat(stream);
}
