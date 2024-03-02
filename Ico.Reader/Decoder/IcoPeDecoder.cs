using Ico.Reader.Data;
using Ico.Reader.Data.Exe;

namespace Ico.Reader.Decoder;
/// <inheritdoc cref="IIcoPeDecoder"/>
public sealed class IcoPeDecoder : IIcoPeDecoder
{
    public DecodedIcoResult? GetDecodedIcoResult(Stream stream, IIcoDecoder icoDecoder)
    {
        var header = MZ_Header.ReadFromStream(stream);
        if (header.Signature[0] != 'M' || header.Signature[1] != 'Z')
            return null;

        var peHeader = PE_Header.ReadFromStream(stream);
        if (peHeader is null)
            return null;

        var readResourceDirectory = ResourceDirectory.ReadFromStream(stream, peHeader);
        if (readResourceDirectory is null)
            return null;

        var icoResource = readResourceDirectory.GetResources(ResourceType.RT_ICON.ToString());
        if (icoResource is null)
            return null;

        var decodedIcoResult = new DecodedIcoResult
        {
            OriginFileType = peHeader.Characteristics.HasFlag(Characteristics.ImageFileDLL) ? IcoOriginFileType.Dll : IcoOriginFileType.Executable,
            References = new ImageReference[icoResource.Length]
        };

        for (int i = 0; i < icoResource.Length; i++)
        {
            var icoDataEntry = icoResource[i];
            var fileOffset = icoDataEntry.GetFileOffset(stream, peHeader);
            var reference = ImageReference.FromStream(stream, fileOffset, icoDataEntry.Size, icoDecoder);
            if (reference is null)
                return null;

            reference.Id = (int)icoDataEntry.ID;
            decodedIcoResult.References[i] = reference;
        }

        var icoResourceGroup = readResourceDirectory.GetResources(ResourceType.RT_GROUP_ICON.ToString());
        if (icoResourceGroup is null)
            return null;

        var icoResourceGroupDirectory = readResourceDirectory.GetDirectory(ResourceType.RT_GROUP_ICON.ToString());
        if (icoResourceGroupDirectory is null)
            return null;

        var groups = new List<IcoGroup>(icoResourceGroup.Length);
        for (int i = 0; i < icoResourceGroup.Length; i++)
        {
            var icoGroup = new IcoGroup()
            {
                Name = icoResourceGroupDirectory.Subdirectories[i].Name
            };

            var fileOffset = icoResourceGroup[i].GetFileOffset(stream, peHeader);
            icoGroup.Header = IcoHeader.ReadFromStream(stream, fileOffset);

            stream.Position = fileOffset + 6;
            var directoryEntries = IcoDirectoryEntry.ReadEntriesFromEXEStream(stream, icoGroup.Header).ToList();

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
            groups.Add(icoGroup);
        }

        decodedIcoResult.IcoGroups = groups.ToArray();

        return decodedIcoResult;
    }

    public bool IsPeFormat(Stream stream)
    {
        var header = MZ_Header.ReadFromStream(stream);
        if (header.Signature[0] != 'M' || header.Signature[1] != 'Z')
            return false;

        return true;
    }
}
