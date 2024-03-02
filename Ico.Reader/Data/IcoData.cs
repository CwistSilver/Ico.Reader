using Ico.Reader.Data.IcoSources;
using Ico.Reader.Decoder;
using System.Collections.ObjectModel;

namespace Ico.Reader.Data;
public class IcoData
{
    /// <summary>
    /// The type of file from which the ico data was originally read.
    /// </summary>
    public IcoOriginFileType OriginFileType { get; }

    /// <summary>
    /// The name associated with the ico data, typically derived from the source file name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A read-only collection of ico groups. For ICO files, which lack natural groupings, ico's are automatically assigned to a single default group (Name 1) to maintain consistency across different sources.
    /// </summary>
    public ReadOnlyCollection<IcoGroup> Groups { get; }

    /// <summary>
    /// A read-only collection of image references, detailing the individual images within the ico data.
    /// </summary>
    public ReadOnlyCollection<ImageReference> ImageReferences { get; }

    private readonly IIcoSource _icoSource;
    private readonly IIcoDecoder _icoDecoder;

    /// <summary>
    /// Initializes a new instance of the IcoData class using a specified ico decoder and ico source, along with a decoded ico result.
    /// </summary>
    /// <param name="icoDecoder">The ico decoder to use for decoding the ico data.</param>
    /// <param name="icoSource">The ico source providing the raw ico data.</param>
    /// <param name="decodedIcoResult">The result of decoding the ico, containing references and ico groups.</param>
    internal IcoData(IIcoDecoder icoDecoder, IIcoSource icoSource, DecodedIcoResult decodedIcoResult)
    {
        _icoDecoder = icoDecoder;
        _icoSource = icoSource;
        ImageReferences = Array.AsReadOnly(decodedIcoResult.References);
        Groups = Array.AsReadOnly(decodedIcoResult.IcoGroups);
        OriginFileType = decodedIcoResult.OriginFileType;
    }

    /// <summary>
    /// Retrieves the image data for a specified image index synchronously.
    /// </summary>
    /// <param name="imageReferenceIndex">The index of the image to retrieve.</param>
    /// <returns>A byte array containing the image data.</returns>
    public byte[] GetImage(int imageReferenceIndex)
    {
        if (imageReferenceIndex < 0 || imageReferenceIndex >= ImageReferences.Count)
            throw new ArgumentOutOfRangeException(nameof(imageReferenceIndex));

        using var stream = _icoSource.GetStream();

        return ImageReferences[imageReferenceIndex].GetImageData(stream, _icoDecoder);
    }

    /// <summary>
    /// Retrieves the image data for a specified group and image index synchronously.
    /// </summary>
    /// <param name="groupName">The name of the ico group.</param>
    /// <param name="entryIndex">The index of the entry within the group to retrieve.</param>
    /// <returns>A byte array containing the image data.</returns>
    public byte[] GetImage(string groupName, int entryIndex)
    {
        var imageReference = GetImageReference(groupName, entryIndex);

        using var stream = _icoSource.GetStream();
        return imageReference.GetImageData(stream, _icoDecoder);
    }

    /// <summary>
    /// Retrieves the image data for a specified image index asynchronously.
    /// </summary>
    /// <param name="imageReferenceIndex">The index of the image to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation and contains the image data as a byte array.</returns>
    public async Task<byte[]> GetImageAsync(int imageReferenceIndex)
    {
        if (imageReferenceIndex < 0 || imageReferenceIndex >= ImageReferences.Count)
            throw new ArgumentOutOfRangeException(nameof(imageReferenceIndex));

        using var stream = _icoSource.GetStream(true);

        stream.Position = ImageReferences[imageReferenceIndex].Offset;
        var imageData = new byte[ImageReferences[imageReferenceIndex].Size];
        await stream.ReadAsync(imageData, 0, imageData.Length);

        return _icoDecoder.GetImageData(imageData.AsSpan(), ImageReferences[imageReferenceIndex].Format);
    }

    /// <summary>
    /// Retrieves the image data for a specified group and image index asynchronously.
    /// </summary>
    /// <param name="groupName">The name of the ico group.</param>
    /// <param name="entryIndex">The index of the entry within the group to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation and contains the image data as a byte array.</returns>
    public async Task<byte[]> GetImageAsync(string groupName, int entryIndex)
    {
        var imageReference = GetImageReference(groupName, entryIndex);

        using var stream = _icoSource.GetStream(true);

        stream.Position = imageReference.Offset;
        var imageData = new byte[imageReference.Size];
        await stream.ReadAsync(imageData, 0, imageData.Length);

        return _icoDecoder.GetImageData(imageData.AsSpan(), imageReference.Format);
    }

    /// <summary>
    /// Saves the image data for a specified image index to a file asynchronously.
    /// </summary>
    /// <param name="imageReferenceIndex">The index of the image to save.</param>
    /// <param name="path">The file path where the image data should be saved.</param>
    public async Task SaveImageAsync(int imageReferenceIndex, string path)
    {
        var data = await GetImageAsync(imageReferenceIndex);
        using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        await fileStream.WriteAsync(data, 0, data.Length);
    }

    /// <summary>
    /// Saves the image data for a specified group and image index to a file asynchronously.
    /// </summary>
    /// <param name="groupName">The id of the ico group.</param>
    /// <param name="entryIndex">The index of the entry within the group to save.</param>
    /// <param name="path">The file path where the image data should be saved.</param>
    public async Task SaveImageAsync(string groupName, int entryIndex, string path)
    {
        var data = await GetImageAsync(groupName, entryIndex);
        using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        await fileStream.WriteAsync(data, 0, data.Length);
    }

    /// <summary>
    /// Determines the index of the preferred image based on its quality, which is calculated using its dimensions and bit depth, adjusted by a specified weight for the color bit depth.
    /// </summary>
    /// <param name="colorBitWeight">The weight to apply to the bit depth in the quality calculation.</param>
    /// <returns>The index of the image with the highest calculated quality.</returns>
    public int PreferredImageIndex(float colorBitWeight = 2f)
    {
        int bestIndex = 0;
        float bestQuality = 0;

        for (int i = 0; i < ImageReferences.Count; i++)
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
    /// Determines the index of the preferred image within a specified ico group based on its quality, which is calculated using its dimensions and bit depth, adjusted by a specified weight for the color bit depth.
    /// </summary>
    /// <param name="groupName">The name of the ico group from which to select the preferred image.</param>
    /// <param name="colorBitWeight">The weight to apply to the bit depth in the quality calculation.</param>
    /// <returns>The index of the image within the global Images collection that corresponds to the highest calculated quality within the specified group.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the specified group name does not exist.</exception>
    public int PreferredImageIndex(string groupName, float colorBitWeight = 2f)
    {
        int bestIndex = 0;
        float bestQuality = 0;

        var group = Groups.FirstOrDefault(x => x.Name == groupName) ?? throw new InvalidOperationException("Group reference not found");
        var imageReferences = new ImageReference[group.Size];

        for (int i = 0; i < group.Size; i++)
            imageReferences[i] = GetImageReference(groupName, i);

        for (int i = 0; i < imageReferences.Length; i++)
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

    /// <summary>
    /// Saves all ico groups to a specified directory asynchronously. Each group is saved in its own subdirectory.
    /// </summary>
    /// <param name="path">The root directory path where the groups should be saved.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SaveAllGroupsToDirectory(string path)
    {
        var rootPath = GetRootDirPath(path);
        var tasks = Groups.Select(group => SaveGroupToDirectory(group.Name, rootPath)).ToArray();

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Saves all images of a specific ico group to a directory asynchronously. The directory is named after the group ID.
    /// </summary>
    /// <param name="groupName">The name of the ico group whose images are to be saved.</param>
    /// <param name="path">The directory path where the images should be saved.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="groupName"/> is out of range.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the group cannot be found.</exception>
    public async Task SaveGroupToDirectory(string groupName, string path)
    {
        var group = Groups.FirstOrDefault(x => x.Name == groupName) ?? throw new InvalidOperationException("Group reference not found");
        var groupPath = Path.Combine(path, $"Group {group.Name}");

        Directory.CreateDirectory(groupPath);

        var tasks = new List<Task>();
        var imageReferences = GetImageReferences(groupName);

        for (int i = 0; i < imageReferences.Count; i++)
        {
            var filePath = GetImageFilePath(imageReferences, i, groupPath);
            tasks.Add(SaveImageAsync(groupName, i, filePath));
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

        for (int i = 0; i < ImageReferences.Count; i++)
        {
            var pathFile = GetImageFilePath(ImageReferences, i, rootPath);
            saveImageTasks.Add(SaveImageAsync(i, pathFile));
        }

        await Task.WhenAll(saveImageTasks);
    }

    /// <summary>
    /// Retrieves the image references for a specified group.
    /// </summary>
    public ReadOnlyCollection<ImageReference> GetImageReferences(string groupName)
    {
        var group = Groups.FirstOrDefault(x => x.Name == groupName) ?? throw new InvalidOperationException("Group reference not found");
        var imageReferences = ImageReferences.Where(x => group.DirectoryEntries!.Any(y => y.RealImageOffset == x.Offset)).ToArray();

        return Array.AsReadOnly(imageReferences);
    }

    /// <summary>
    /// Retrieves the image reference for a specified group and image index.
    /// </summary>
    public ImageReference GetImageReference(string groupName, int imageIndex)
    {
        if (imageIndex < 0 || imageIndex >= ImageReferences.Count)
            throw new ArgumentOutOfRangeException(nameof(imageIndex));

        var group = Groups.FirstOrDefault(x => x.Name == groupName) ?? throw new InvalidOperationException("Group reference not found");
        var entry = group.DirectoryEntries![imageIndex];

        return ImageReferences.FirstOrDefault(x => x.Offset == entry.RealImageOffset) ?? throw new InvalidOperationException("Image reference not found");
    }

    public override string ToString() => $"{Name} Groups[{Groups.Count}] Images[{ImageReferences.Count}] ({OriginFileType})";

    private string GetRootDirPath(string path)
    {
        var rootName = string.IsNullOrEmpty(Name) ? $"Ico_{DateTime.Now.Day}-{DateTime.Now.Month}-{DateTime.Now.Year} {DateTime.Now.Hour}-{DateTime.Now.Minute}-{DateTime.Now.Second}" : Name;
        return Path.Combine(path, rootName);
    }

    private string GetImageFilePath(ReadOnlyCollection<ImageReference> imageReferences, int index, string rootPath)
    {
        var fileName = string.IsNullOrEmpty(Name) ? $"{imageReferences[index].Id}_image ({imageReferences[index].Width}x{imageReferences[index].Height} {imageReferences[index].BitCount} bit).png" : $"{imageReferences[index].Id}_{Name} ({imageReferences[index].Width}x{imageReferences[index].Height} {imageReferences[index].BitCount} bit).png";
        return Path.Combine(rootPath, fileName);
    }
}
