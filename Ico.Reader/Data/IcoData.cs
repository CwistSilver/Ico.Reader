using System.Collections.ObjectModel;

using Ico.Reader.Data.Source;
using Ico.Reader.Decoder;

namespace Ico.Reader.Data;

public class IcoData
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
        IconGroups = Array.AsReadOnly(decodedIcoResult.IcoGroups.Where(x => x.IcoType == IcoType.Icon).Cast<IconGroup>().ToArray());
        CursorGroups = Array.AsReadOnly(decodedIcoResult.IcoGroups.Where(x => x.IcoType == IcoType.Cursor).Cast<CursorGroup>().ToArray());
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
        return imageReference.GetImageData(stream, _icoDecoder);
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
    public Task<byte[]> GetImageAsync(int imageReferenceIndex) => GetImageAsync(ImageReferences[imageReferenceIndex]);

    /// <summary>
    /// Retrieves the image data for a specified group and image index asynchronously.
    /// </summary>
    /// <param name="groupName">The name of the ICO group.</param>
    /// <param name="entryIndex">The index of the entry within the group to retrieve.</param>
    /// <param name="icoType">The ICO type (Icon or Cursor) to specify the image type.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The result contains a byte array with the image data.
    /// </returns>
    public Task<byte[]> GetImageAsync(string groupName, int entryIndex, IcoType icoType)
    {
        var imageReference = GetImageReference(groupName, entryIndex, icoType);
        return GetImageAsync(imageReference);
    }

    /// <summary>
    /// Retrieves the image data for a specified entry within an ICO group asynchronously.
    /// </summary>
    /// <param name="group">The ICO group that contains the image entry.</param>
    /// <param name="entryIndex">The index of the entry within the group.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The result contains a byte array with the image data.
    /// </returns>
    public Task<byte[]> GetImageAsync(IIcoGroup group, int entryIndex)
    {
        var imageReference = GetImageReference(group, entryIndex);
        return GetImageAsync(imageReference);
    }

    /// <summary>
    /// Retrieves the image data for a specified image reference asynchronously.
    /// </summary>
    /// <param name="imageReference">The image reference that contains metadata for the image.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The result contains a byte array with the image data.
    /// </returns>
    public async Task<byte[]> GetImageAsync(ImageReference imageReference)
    {
        using var stream = _dataSource.GetStream(true);
        stream.Position = imageReference.Offset;
        var imageData = new byte[imageReference.Size];
        await stream.ReadAsync(imageData, 0, imageData.Length);

        return _icoDecoder.GetImageData(imageData.AsSpan(), imageReference.Format);
    }

    #endregion

    #region SaveImageAsyncFunctions
    /// <summary>
    /// Saves the image data for a specified image index to a file asynchronously.
    /// </summary>
    /// <param name="imageReferenceIndex">The index of the image to save.</param>
    /// <param name="path">The file path where the image data should be saved.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    public Task SaveImageAsync(int imageReferenceIndex, string path) => SaveImageAsync(ImageReferences[imageReferenceIndex], path);

    /// <summary>
    /// Saves the image data for a specified group and image index to a file asynchronously.
    /// </summary>
    /// <param name="groupName">The name of the ICO group.</param>
    /// <param name="entryIndex">The index of the entry within the group to save.</param>
    /// <param name="path">The file path where the image data should be saved.</param>
    /// <param name="icoType">The ICO type (Icon or Cursor) to specify the image type.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    public Task SaveImageAsync(string groupName, int entryIndex, string path, IcoType icoType)
    {
        var imageReference = GetImageReference(groupName, entryIndex, icoType);
        return SaveImageAsync(imageReference, path);
    }

    /// <summary>
    /// Saves the image data for a specified entry within an ICO group to a file asynchronously.
    /// </summary>
    /// <param name="group">The ICO group that contains the image entry.</param>
    /// <param name="entryIndex">The index of the entry within the group to save.</param>
    /// <param name="path">The file path where the image data should be saved.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    public Task SaveImageAsync(IIcoGroup group, int entryIndex, string path)
    {
        var imageReference = GetImageReference(group, entryIndex);
        return SaveImageAsync(imageReference, path);
    }

    /// <summary>
    /// Saves the image data for a specified image reference to a file asynchronously.
    /// </summary>
    /// <param name="imageReference">The image reference that contains metadata for the image.</param>
    /// <param name="path">The file path where the image data should be saved.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    public async Task SaveImageAsync(ImageReference imageReference, string path)
    {
        var data = await GetImageAsync(imageReference);
        using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 4096, true);
        await fileStream.WriteAsync(data, 0, data.Length);
    }

    #endregion

    #region PreferredImageIndexFunctions

    /// <summary>
    /// Determines the index of the preferred image based on its quality, calculated using its dimensions and bit depth,
    /// adjusted by a specified weight for the color bit depth.
    /// </summary>
    /// <param name="colorBitWeight">The weight to apply to the bit depth in the quality calculation.</param>
    /// <returns>The index of the image with the highest calculated quality.</returns>
    public int PreferredImageIndex(float colorBitWeight = 2f)
    {
        var bestIndex = 0;
        float bestQuality = 0;

        for (var i = 0; i < ImageReferences.Count; i++)
        {
            var imageReference = ImageReferences[i];
            var qualityScore = imageReference.Width * imageReference.Height * (imageReference.BitCount * colorBitWeight);

            if (qualityScore > bestQuality)
            {
                bestQuality = qualityScore;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    /// <summary>
    /// Determines the index of the preferred image for a given group based on its quality.
    /// </summary>
    /// <param name="groupName">The name of the ICO group.</param>
    /// <param name="icoType">The ICO type (Icon or Cursor) to specify the image type.</param>
    /// <param name="colorBitWeight">The weight to apply to the bit depth in the quality calculation.</param>
    /// <returns>The index of the preferred image within the global image reference list (<see cref="ImageReferences"/>).</returns>
    public int PreferredImageIndex(string groupName, IcoType icoType, float colorBitWeight = 2f)
    {
        var group = GetGroup(groupName, icoType);
        return PreferredImageIndex(group, colorBitWeight);
    }

    /// <summary>
    /// Determines the index of the preferred image within a specified ICO group based on its quality.
    /// </summary>
    /// <param name="group">The ICO group containing the images.</param>
    /// <param name="colorBitWeight">The weight to apply to the bit depth in the quality calculation.</param>
    /// <returns>The index of the preferred image within the global image reference list (<see cref="ImageReferences"/>).</returns>
    public int PreferredImageIndex(IIcoGroup group, float colorBitWeight = 2f)
    {
        var bestIndex = 0;
        float bestQuality = 0;

        var imageReferences = new ImageReference[group.Size];
        for (var i = 0; i < group.Size; i++)
            imageReferences[i] = GetImageReference(group, i);

        for (var i = 0; i < imageReferences.Length; i++)
        {
            var imageReference = imageReferences[i];
            var qualityScore = imageReference.Width * imageReference.Height * (imageReference.BitCount * colorBitWeight);

            if (qualityScore > bestQuality)
            {
                bestQuality = qualityScore;
                bestIndex = i;
            }
        }

        return ImageReferences.IndexOf(imageReferences[bestIndex]);
    }

    #endregion

    #region SaveFunctions

    /// <summary>
    /// Saves all ICO groups to a specified directory asynchronously. Each group is saved in its own subdirectory.
    /// </summary>
    /// <param name="path">The root directory path where the groups should be saved.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SaveAllGroupsToDirectory(string path)
    {
        var rootPath = GetRootDirPath(path);
        var tasks = Groups.Select(group => SaveGroupToDirectory(group.Name, rootPath, group.IcoType)).ToArray();
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Saves all images of a specific ICO group to a directory asynchronously.
    /// </summary>
    /// <param name="groupName">The name of the ICO group whose images are to be saved.</param>
    /// <param name="path">The directory path where the images should be saved.</param>
    /// <param name="icoType">The ICO type (Icon or Cursor) to specify the group type.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the specified ICO group is not found.</exception>
    public Task SaveGroupToDirectory(string groupName, string path, IcoType icoType)
    {
        var group = GetGroup(groupName, icoType);
        return SaveGroupToDirectory(group, path);
    }

    /// <summary>
    /// Saves all images of a specified ICO group to a directory asynchronously.
    /// </summary>
    /// <param name="group">The ICO group whose images are to be saved.</param>
    /// <param name="path">The directory path where the images should be saved.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SaveGroupToDirectory(IIcoGroup group, string path)
    {
        var tasks = new List<Task>();
        var groupPath = Path.Combine(path, group.IcoType.ToString(), $"Group {group.Name}");
        Directory.CreateDirectory(groupPath);
        var imageReferences = GetImageReferences(group);

        for (var i = 0; i < imageReferences.Count; i++)
        {
            var imageReference = imageReferences[i];
            var filePath = GetImageFilePath(imageReference, groupPath);
            tasks.Add(SaveImageAsync(imageReference, filePath));
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Saves all images to a specified directory asynchronously. Each image is saved as a separate file.
    /// </summary>
    /// <param name="path">The directory path where the images should be saved.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SaveAllImagesToDirectory(string path)
    {
        var rootPath = GetRootDirPath(path);
        Directory.CreateDirectory(GetRootDirPath(path));

        var saveImageTasks = new List<Task>();

        for (var i = 0; i < ImageReferences.Count; i++)
        {
            var imageReference = ImageReferences[i];
            var pathFile = GetImageFilePath(imageReference, rootPath);
            saveImageTasks.Add(SaveImageAsync(imageReference, pathFile));
        }

        await Task.WhenAll(saveImageTasks);
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
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="imageIndex"/> is out of range.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the image reference is not found.</exception>
    public ImageReference GetImageReference(IIcoGroup group, int imageIndex)
    {
        if (imageIndex < 0 || imageIndex >= ImageReferences.Count)
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

    private string GetRootDirPath(string path)
    {
        var rootName = string.IsNullOrEmpty(Name) ? $"Ico_{DateTime.Now.Day}-{DateTime.Now.Month}-{DateTime.Now.Year} {DateTime.Now.Hour}-{DateTime.Now.Minute}-{DateTime.Now.Second}" : Name;
        return Path.Combine(path, rootName);
    }

    private string GetImageFilePath(ImageReference imageReference, string rootPath)
    {
        string fileName;
        if (string.IsNullOrEmpty(Name))
            fileName = $"{imageReference.Id}_{imageReference.IcoType} ({imageReference.Width}x{imageReference.Height} {imageReference.BitCount} bit).png";
        else
            fileName = $"{imageReference.Id}_{Name} ({imageReference.Width}x{imageReference.Height} {imageReference.BitCount} bit).png";

        return Path.Combine(rootPath, fileName);
    }

    #region Obsolete

    /// <summary>
    /// Retrieves the image data for a specified group and image index synchronously.
    /// </summary>
    /// <param name="groupName">The name of the ICO group.</param>
    /// <param name="entryIndex">The index of the entry within the group to retrieve.</param>
    /// <returns>A byte array containing the image data.</returns>
    /// <remarks>
    /// This method is obsolete and will be removed in a future version. 
    /// Use <see cref="GetImage(string, int, IcoType)"/> or <see cref="GetImage(IIcoGroup, int)"/> instead.
    /// </remarks>
    [Obsolete($@"Use {nameof(GetImage)}(string groupName, int entryIndex, {nameof(IcoType)} icoType) or {nameof(GetImage)}(IIcoGroup group, int entryIndex) instead.
              This overload implicitly assumes {nameof(IcoType)}.{nameof(IcoType.Icon)}, which may result in referencing the wrong image.", false)]
    public byte[] GetImage(string groupName, int entryIndex) => GetImage(groupName, entryIndex, IcoType.Icon);

    /// <summary>
    /// Retrieves the image data for a specified group and image index asynchronously.
    /// </summary>
    /// <param name="groupName">The name of the ICO group.</param>
    /// <param name="entryIndex">The index of the entry within the group to retrieve.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The result contains a byte array with the image data.
    /// </returns>
    /// <remarks>
    /// This method is obsolete and will be removed in a future version. 
    /// Use <see cref="GetImageAsync(string, int, IcoType)"/> or <see cref="GetImageAsync(IIcoGroup, int)"/> instead.
    /// </remarks>
    [Obsolete($@"Use {nameof(GetImageAsync)}(string groupName, int entryIndex, {nameof(IcoType)} icoType) or 
            {nameof(GetImageAsync)}({nameof(IIcoGroup)} group, int entryIndex) instead.
            This overload implicitly assumes {nameof(IcoType)}.{nameof(IcoType.Icon)}, which may result in referencing the wrong image.", false)]
    public Task<byte[]> GetImageAsync(string groupName, int entryIndex) => GetImageAsync(groupName, entryIndex, IcoType.Icon);

    /// <summary>
    /// Saves the image data for a specified group and image index to a file asynchronously.
    /// </summary>
    /// <param name="groupName">The name of the ICO group.</param>
    /// <param name="entryIndex">The index of the entry within the group to save.</param>
    /// <param name="path">The file path where the image data should be saved.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    /// <remarks>
    /// This method is obsolete and will be removed in a future version.  
    /// Use <see cref="SaveImageAsync(string, int, string, IcoType)"/> or <see cref="SaveImageAsync(IIcoGroup, int, string)"/> instead.
    /// </remarks>
    [Obsolete($@"Use {nameof(SaveImageAsync)}(string groupName, int entryIndex, string path, {nameof(IcoType)} icoType) or 
            {nameof(SaveImageAsync)}({nameof(IIcoGroup)} group, int entryIndex, string path) instead.
            This overload implicitly assumes {nameof(IcoType)}.{nameof(IcoType.Icon)}, which may result in saving the wrong image.", false)]
    public Task SaveImageAsync(string groupName, int entryIndex, string path) => SaveImageAsync(groupName, entryIndex, path, IcoType.Icon);

    /// <summary>
    /// Determines the index of the preferred image for a given group based on its quality, using a default ICO type of <see cref="IcoType.Icon"/>.
    /// </summary>
    /// <param name="groupName">The name of the ICO group.</param>
    /// <param name="colorBitWeight">The weight to apply to the bit depth in the quality calculation.</param>
    /// <returns>The index of the preferred image within the specified group.</returns>
    /// <remarks>
    /// This method is obsolete and will be removed in a future version.
    /// Use <see cref="PreferredImageIndex(string, IcoType, float)"/> or <see cref="PreferredImageIndex(IIcoGroup, float)"/> instead.
    /// </remarks>
    [Obsolete($@"Use {nameof(PreferredImageIndex)}(string groupName, {nameof(IcoType)} icoType, float colorBitWeight) or 
            {nameof(PreferredImageIndex)}({nameof(IIcoGroup)} group, float colorBitWeight) instead.
            This overload implicitly assumes {nameof(IcoType)}.{nameof(IcoType.Icon)}, which may result in selecting the wrong image.", false)]
    public int PreferredImageIndex(string groupName, float colorBitWeight = 2f) => PreferredImageIndex(groupName, IcoType.Icon, colorBitWeight);

    /// <summary>
    /// Saves all images of a specific ICO group to a directory asynchronously.
    /// The directory is named after the group ID.
    /// </summary>
    /// <param name="groupName">The name of the ICO group whose images are to be saved.</param>
    /// <param name="path">The directory path where the images should be saved.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="groupName"/> is out of range.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the group cannot be found.</exception>
    /// <remarks>
    /// This method is obsolete and will be removed in a future version.
    /// Use <see cref="SaveGroupToDirectory(string, string, IcoType)"/> or <see cref="SaveGroupToDirectory(IIcoGroup, string)"/> instead.
    /// </remarks>
    [Obsolete($@"Use {nameof(SaveGroupToDirectory)}(string groupName, string path, {nameof(IcoType)} icoType) or 
                {nameof(SaveGroupToDirectory)}({nameof(IIcoGroup)} group, string path) instead.
                This overload implicitly assumes {nameof(IcoType)}.{nameof(IcoType.Icon)}, which may result in saving the wrong group.", false)]
    public Task SaveGroupToDirectory(string groupName, string path) => SaveGroupToDirectory(groupName, path, IcoType.Icon);

    /// <summary>
    /// Retrieves the image references for a specified group.
    /// </summary>
    /// <param name="groupName">The name of the ICO group.</param>
    /// <returns>
    /// A read-only collection of <see cref="ImageReference"/> objects associated with the specified group.
    /// </returns>
    /// <remarks>
    /// This method is obsolete and will be removed in a future version.
    /// Use <see cref="GetImageReferences(string, IcoType)"/> or <see cref="GetImageReferences(IIcoGroup)"/> instead.
    /// </remarks>
    [Obsolete($@"Use {nameof(GetImageReferences)}(string groupName, {nameof(IcoType)} icoType) or 
            {nameof(GetImageReferences)}({nameof(IIcoGroup)} group) instead.
            This overload implicitly assumes {nameof(IcoType)}.{nameof(IcoType.Icon)}, which may result in referencing the wrong group.", false)]
    public ReadOnlyCollection<ImageReference> GetImageReferences(string groupName) => GetImageReferences(groupName, IcoType.Icon);

    /// <summary>
    /// Retrieves the image reference for a specified group and image index.
    /// </summary>
    /// <param name="groupName">The name of the ICO group.</param>
    /// <param name="imageIndex">The index of the image within the group.</param>
    /// <returns>The <see cref="ImageReference"/> associated with the specified group and index.</returns>
    /// <remarks>
    /// This method is obsolete and will be removed in a future version.
    /// Use <see cref="GetImageReference(string, int, IcoType)"/> or <see cref="GetImageReference(IIcoGroup, int)"/> instead.
    /// </remarks>
    [Obsolete($@"Use {nameof(GetImageReference)}(string groupName, int imageIndex, {nameof(IcoType)} icoType) or 
            {nameof(GetImageReference)}({nameof(IIcoGroup)} group, int imageIndex) instead.
            This overload implicitly assumes {nameof(IcoType)}.{nameof(IcoType.Icon)}, which may result in referencing the wrong image.", false)]
    public ImageReference GetImageReference(string groupName, int imageIndex) => GetImageReference(groupName, imageIndex, IcoType.Icon);

    #endregion
}
