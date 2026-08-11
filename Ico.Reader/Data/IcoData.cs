using System.Collections.ObjectModel;

using Ico.Reader.Data.Source;
using Ico.Reader.Decoder;
using Ico.Reader.Reading;

namespace Ico.Reader.Data;

public sealed class IcoData
{
    /// <summary>
    /// The type of file from which the ICO data was originally extracted.
    /// </summary>
    public IcoOriginFileType OriginFileType { get; }

    /// <summary>
    /// The name associated with the ICO data, typically derived from the source file name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A read-only collection of ICO groups.
    /// <para>
    /// For ICO files, which do not have inherent groupings, all icons are automatically assigned 
    /// to a single default group (named "1") to ensure consistency across different sources.
    /// </para>
    /// <para>
    /// Use <see cref="IconGroups"/> or <see cref="CursorGroups"/> to access groups by type.
    /// </para>
    /// </summary>
    public ReadOnlyCollection<IIcoGroup> Groups { get; }

    /// <summary>
    /// A read-only collection of icon groups, containing only ICO entries.
    /// <para>
    /// This collection includes groups of type <see cref="IcoType.Icon"/>.
    /// </para>
    /// </summary>
    public ReadOnlyCollection<IconGroup> IconGroups { get; }

    /// <summary>
    /// A read-only collection of cursor groups, containing only CUR entries.
    /// <para>
    /// This collection includes groups of type <see cref="IcoType.Cursor"/>.
    /// </para>
    /// </summary>
    public ReadOnlyCollection<CursorGroup> CursorGroups { get; }

    /// <summary>
    /// A read-only collection of image references, detailing individual images within the ICO data.
    /// </summary>
    public ReadOnlyCollection<ImageReference> ImageReferences { get; }

    private readonly IDataSource _dataSource;
    private readonly IIcoDecoder _icoDecoder;

    internal IcoData(IIcoDecoder icoDecoder, IDataSource dataSource, DecodedIcoResult decodedIcoResult)
    {
        _icoDecoder = icoDecoder;
        _dataSource = dataSource;
        ImageReferences = Array.AsReadOnly(decodedIcoResult.References.ToArray());
        Groups = Array.AsReadOnly(decodedIcoResult.IcoGroups.ToArray());
        IconGroups = Array.AsReadOnly(decodedIcoResult.IcoGroups.OfType<IconGroup>().ToArray());
        CursorGroups = Array.AsReadOnly(decodedIcoResult.IcoGroups.OfType<CursorGroup>().ToArray());
        OriginFileType = decodedIcoResult.OriginFileType;
    }

    #region GetImageFunctions
    /// <summary>
    /// Retrieves the image data for a specified image reference synchronously.
    /// </summary>
    /// <param name="imageReference">The image reference that contains metadata for the image.</param>
    /// <returns>A byte array containing the image data.</returns>
    public byte[] GetImage(ImageReference imageReference)
    {
        using var stream = _dataSource.GetStream();
        return ImageDataReader.Read(stream, imageReference, _icoDecoder);
    }

    /// <summary>
    /// Retrieves the image data for a specified group and image index synchronously.
    /// </summary>
    /// <param name="groupName">The name of the ICO group.</param>
    /// <param name="entryIndex">The index of the entry within the group to retrieve.</param>
    /// <param name="icoType">The ICO type (Icon or Cursor) to specify the image type.</param>
    /// <returns>A byte array containing the image data.</returns>
    public byte[] GetImage(string groupName, int entryIndex, IcoType icoType)
    {
        var imageReference = GetImageReference(groupName, entryIndex, icoType);
        return GetImage(imageReference);
    }

    /// <summary>
    /// Retrieves the image data for a specified entry within an ICO group.
    /// </summary>
    /// <param name="group">The ICO group that contains the image entry.</param>
    /// <param name="entryIndex">The index of the entry within the group.</param>
    /// <returns>A byte array containing the image data.</returns>
    public byte[] GetImage(IIcoGroup group, int entryIndex)
    {
        var imageReference = GetImageReference(group, entryIndex);
        return GetImage(imageReference);
    }

    /// <summary>
    /// Retrieves the image data for a specified image index synchronously.
    /// </summary>
    /// <param name="imageReferenceIndex">The index of the image to retrieve.</param>
    /// <returns>A byte array containing the image data.</returns>
    public byte[] GetImage(int imageReferenceIndex) => GetImage(ImageReferences[imageReferenceIndex]);

    #endregion

    #region GetImageAsyncFunctions
    /// <summary>
    /// Retrieves the image data for a specified image index asynchronously.
    /// </summary>
    /// <param name="imageReferenceIndex">The index of the image to retrieve.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The result contains a byte array with the image data.
    /// </returns>
    public Task<byte[]> GetImageAsync(int imageReferenceIndex, CancellationToken cancellationToken = default)
        => GetImageAsync(ImageReferences[imageReferenceIndex], cancellationToken);

    /// <summary>
    /// Retrieves the image data for a specified group and image index asynchronously.
    /// </summary>
    /// <param name="groupName">The name of the ICO group.</param>
    /// <param name="entryIndex">The index of the entry within the group to retrieve.</param>
    /// <param name="icoType">The ICO type (Icon or Cursor) to specify the image type.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The result contains a byte array with the image data.
    /// </returns>
    public Task<byte[]> GetImageAsync(string groupName, int entryIndex, IcoType icoType, CancellationToken cancellationToken = default)
        => GetImageAsync(GetImageReference(groupName, entryIndex, icoType), cancellationToken);

    /// <summary>
    /// Retrieves the image data for a specified entry within an ICO group asynchronously.
    /// </summary>
    /// <param name="group">The ICO group that contains the image entry.</param>
    /// <param name="entryIndex">The index of the entry within the group.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The result contains a byte array with the image data.
    /// </returns>
    public Task<byte[]> GetImageAsync(IIcoGroup group, int entryIndex, CancellationToken cancellationToken = default)
        => GetImageAsync(GetImageReference(group, entryIndex), cancellationToken);

    /// <summary>
    /// Retrieves the image data for a specified image reference asynchronously.
    /// </summary>
    /// <param name="imageReference">The image reference that contains metadata for the image.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The result contains a byte array with the image data.
    /// </returns>
    public async Task<byte[]> GetImageAsync(ImageReference imageReference, CancellationToken cancellationToken = default)
    {
        using var stream = _dataSource.GetStream(useAsync: true);
        return await ImageDataReader.ReadAsync(stream, imageReference, _icoDecoder, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region PreferredImageIndexFunctions

    /// <summary>
    /// Determines the index of the preferred image, scoring pixel area and color bit depth relative to the best
    /// value present and combining them using the supplied weights.
    /// </summary>
    /// <param name="colorBitWeight">The relative importance of the color bit depth.</param>
    /// <param name="areaWeight">The relative importance of the pixel area.</param>
    /// <returns>The index of the image with the highest calculated quality, or -1 if there are no images.</returns>
    public int PreferredImageIndex(float colorBitWeight = 1f, float areaWeight = 2f)
        => ImageQuality.BestIndex(ImageReferences, colorBitWeight, areaWeight);

    /// <summary>
    /// Determines the index of the preferred image for a given group based on its quality.
    /// </summary>
    /// <param name="groupName">The name of the ICO group.</param>
    /// <param name="icoType">The ICO type (Icon or Cursor) to specify the image type.</param>
    /// <param name="colorBitWeight">The relative importance of the color bit depth.</param>
    /// <param name="areaWeight">The relative importance of the pixel area.</param>
    /// <returns>The index of the preferred image within the global image reference list (<see cref="ImageReferences"/>), or -1 if the group is empty.</returns>
    public int PreferredImageIndex(string groupName, IcoType icoType, float colorBitWeight = 1f, float areaWeight = 2f)
    {
        var group = GetGroup(groupName, icoType);
        return PreferredImageIndex(group, colorBitWeight, areaWeight);
    }

    /// <summary>
    /// Determines the index of the preferred image within a specified ICO group based on its quality.
    /// </summary>
    /// <param name="group">The ICO group containing the images.</param>
    /// <param name="colorBitWeight">The relative importance of the color bit depth.</param>
    /// <param name="areaWeight">The relative importance of the pixel area.</param>
    /// <returns>The index of the preferred image within the global image reference list (<see cref="ImageReferences"/>), or -1 if the group is empty.</returns>
    public int PreferredImageIndex(IIcoGroup group, float colorBitWeight = 1f, float areaWeight = 2f)
    {
        var imageReferences = GetImageReferences(group);
        var bestIndex = ImageQuality.BestIndex(imageReferences, colorBitWeight, areaWeight);
        return bestIndex < 0 ? -1 : ImageReferences.IndexOf(imageReferences[bestIndex]);
    }

    #endregion

    #region GetImageReferenceFunctions

    /// <summary>
    /// Retrieves the image reference for a specified group and image index.
    /// </summary>
    /// <param name="groupName">The name of the ICO group.</param>
    /// <param name="imageIndex">The index of the image within the group.</param>
    /// <param name="icoType">The ICO type (Icon or Cursor) to specify the group type.</param>
    /// <returns>The <see cref="ImageReference"/> associated with the specified group and index.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the specified ICO group is not found.</exception>
    public ImageReference GetImageReference(string groupName, int imageIndex, IcoType icoType)
    {
        var group = GetGroup(groupName, icoType);
        return GetImageReference(group, imageIndex);
    }

    /// <summary>
    /// Retrieves the image reference for a specified ICO group and image index.
    /// </summary>
    /// <param name="group">The ICO group containing the image.</param>
    /// <param name="imageIndex">The index of the image within the group.</param>
    /// <returns>The <see cref="ImageReference"/> associated with the specified group and index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="imageIndex"/> is outside the group's entries.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the image reference is not found.</exception>
    public ImageReference GetImageReference(IIcoGroup group, int imageIndex)
    {
        if (imageIndex < 0 || imageIndex >= group.Size)
            throw new ArgumentOutOfRangeException(nameof(imageIndex));

        var entry = group.DirectoryEntries![imageIndex];

        return ImageReferences.FirstOrDefault(x => x.Offset == entry.RealImageOffset) ?? throw new InvalidOperationException("Image reference not found");
    }

    #endregion

    #region GetImageReferencesFunctions

    /// <summary>
    /// Retrieves the image references for a specified ICO group.
    /// </summary>
    /// <param name="groupName">The name of the ICO group.</param>
    /// <param name="icoType">The ICO type (Icon or Cursor) to specify the group type.</param>
    /// <returns>
    /// A read-only collection of <see cref="ImageReference"/> objects associated with the specified group.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown if the specified ICO group is not found.</exception>
    public ReadOnlyCollection<ImageReference> GetImageReferences(string groupName, IcoType icoType)
    {
        var group = GetGroup(groupName, icoType);
        return GetImageReferences(group);
    }

    /// <summary>
    /// Retrieves the image references for a specified ICO group.
    /// </summary>
    /// <param name="group">The ICO group whose images are to be retrieved.</param>
    /// <returns>
    /// A read-only collection of <see cref="ImageReference"/> objects associated with the specified group.
    /// </returns>
    public ReadOnlyCollection<ImageReference> GetImageReferences(IIcoGroup group)
    {
        var imageReferences = ImageReferences.Where(x => group.DirectoryEntries!.Any(y => y.RealImageOffset == x.Offset)).ToArray();
        return Array.AsReadOnly(imageReferences);
    }

    #endregion

    /// <summary>
    /// Retrieves the ICO groups with the specified name.
    /// </summary>
    /// <param name="groupName"> The name of the ICO group.</param>
    /// <returns></returns>
    public IEnumerable<IIcoGroup> GetGroups(string groupName)
        => Groups.Where(x => x.Name == groupName);

    /// <summary>
    /// Retrieves the ICO group with the specified name.
    /// </summary>
    /// <param name="groupName"> The name of the ICO group.</param>
    /// <param name="icoType"> The <see cref="IcoType"/> to specify the group type.</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public IIcoGroup GetGroup(string groupName, IcoType icoType)
        => Groups.FirstOrDefault(x => x.IcoType == icoType && x.Name == groupName) ?? throw new InvalidOperationException("Group reference not found");

    /// <summary>
    /// Retrieves the <see cref="IconGroup"/> with the specified name.
    /// </summary>
    /// <param name="groupName"> The name of the ICO group.</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public IconGroup GetIconGroup(string groupName)
        => IconGroups.FirstOrDefault(x => x.Name == groupName) ?? throw new InvalidOperationException("Group reference not found");

    /// <summary>
    /// Retrieves the <see cref="CursorGroup"/> with the specified name.
    /// </summary>
    /// <param name="groupName"> The name of the ICO group.</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public CursorGroup GetCursorGroup(string groupName)
        => CursorGroups.FirstOrDefault(x => x.Name == groupName) ?? throw new InvalidOperationException("Group reference not found");

    public override string ToString() => $"{Name} Groups[{Groups.Count}] Images[{ImageReferences.Count}] ({OriginFileType})";

}
