using Ico.Reader.Data;

namespace Ico.Reader.Export;

/// <inheritdoc cref="IIcoExporter"/>
public sealed class IcoExporter : IIcoExporter
{
    private const char Replacement = '_';

    /// <summary>
    /// The characters Windows refuses in a file name, which include both path separators. They are replaced on every
    /// platform, so an export has the same layout wherever it runs.
    /// </summary>
    private static readonly HashSet<char> _invalidFileNameCharacters =
        [.. Enumerable.Range(0, 32).Select(code => (char)code), '"', '*', '/', ':', '<', '>', '?', '\\', '|'];

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
        var groupPath = Path.Combine(path, group.IcoType.ToString(), $"Group {ToFileName(group.Name)}");
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
            : ToFileName(icoData.Name);

        return Path.Combine(path, rootName);
    }

    /// <summary>
    /// An image id is only unique within its type, as an EXE or DLL numbers icons and cursors separately, so the type is
    /// part of every file name.
    /// </summary>
    private static string GetImageFilePath(IcoData icoData, ImageReference imageReference, string rootPath)
    {
        var prefix = string.IsNullOrEmpty(icoData.Name) ? imageReference.IcoType.ToString() : $"{ToFileName(icoData.Name)}_{imageReference.IcoType}";
        var fileName = $"{imageReference.Id}_{prefix} ({imageReference.Width}x{imageReference.Height} {imageReference.BitCount} bit).png";

        return Path.Combine(rootPath, fileName);
    }

    /// <summary>
    /// Group names come from the file being read, so they can hold path separators or characters no file system
    /// accepts. Windows also ignores dots and spaces at the end of a name, which would turn ".." into the parent
    /// directory, so those are dropped and a name left empty is replaced.
    /// </summary>
    private static string ToFileName(string name)
    {
        var characters = name.Select(character => _invalidFileNameCharacters.Contains(character) ? Replacement : character).ToArray();
        var fileName = new string(characters).TrimEnd('.', ' ');

        return fileName.Length == 0 ? Replacement.ToString() : fileName;
    }
}
