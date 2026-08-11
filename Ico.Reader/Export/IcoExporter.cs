using Ico.Reader.Data;

namespace Ico.Reader.Export;

/// <inheritdoc cref="IIcoExporter"/>
public sealed class IcoExporter : IIcoExporter
{
    /// <inheritdoc/>
    public async Task SaveImageAsync(IcoData icoData, ImageReference imageReference, string path, CancellationToken cancellationToken = default)
    {
        var data = await icoData.GetImageAsync(imageReference, cancellationToken).ConfigureAwait(false);

        using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 4096, useAsync: true);
        await fileStream.WriteAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task SaveGroupToDirectoryAsync(IcoData icoData, IIcoGroup group, string path, CancellationToken cancellationToken = default)
    {
        var groupPath = Path.Combine(path, group.IcoType.ToString(), $"Group {group.Name}");
        Directory.CreateDirectory(groupPath);

        var imageReferences = icoData.GetImageReferences(group);

        return SaveEachAsync(icoData, imageReferences, groupPath, cancellationToken);
    }

    /// <inheritdoc/>
    public Task SaveAllGroupsToDirectoryAsync(IcoData icoData, string path, CancellationToken cancellationToken = default)
    {
        var rootPath = GetRootDirectoryPath(icoData, path);
        var tasks = icoData.Groups.Select(group => SaveGroupToDirectoryAsync(icoData, group, rootPath, cancellationToken));

        return Task.WhenAll(tasks);
    }

    /// <inheritdoc/>
    public Task SaveAllImagesToDirectoryAsync(IcoData icoData, string path, CancellationToken cancellationToken = default)
    {
        var rootPath = GetRootDirectoryPath(icoData, path);
        Directory.CreateDirectory(rootPath);

        return SaveEachAsync(icoData, icoData.ImageReferences, rootPath, cancellationToken);
    }

    private Task SaveEachAsync(IcoData icoData, IReadOnlyList<ImageReference> imageReferences, string directory, CancellationToken cancellationToken)
    {
        var tasks = new List<Task>(imageReferences.Count);

        foreach (var imageReference in imageReferences)
        {
            cancellationToken.ThrowIfCancellationRequested();
            tasks.Add(SaveImageAsync(icoData, imageReference, GetImageFilePath(icoData, imageReference, directory), cancellationToken));
        }

        return Task.WhenAll(tasks);
    }

    /// <summary>
    /// Ico data read from a byte array or a stream has no name, so the directory falls back to a
    /// timestamp to keep separate exports apart.
    /// </summary>
    private static string GetRootDirectoryPath(IcoData icoData, string path)
    {
        var rootName = string.IsNullOrEmpty(icoData.Name)
            ? $"Ico_{DateTime.Now:dd-MM-yyyy HH-mm-ss}"
            : icoData.Name;

        return Path.Combine(path, rootName);
    }

    private static string GetImageFilePath(IcoData icoData, ImageReference imageReference, string rootPath)
    {
        var prefix = string.IsNullOrEmpty(icoData.Name) ? imageReference.IcoType.ToString() : icoData.Name;
        var fileName = $"{imageReference.Id}_{prefix} ({imageReference.Width}x{imageReference.Height} {imageReference.BitCount} bit).png";

        return Path.Combine(rootPath, fileName);
    }
}
