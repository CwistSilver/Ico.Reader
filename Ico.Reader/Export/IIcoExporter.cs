using Ico.Reader.Data;

namespace Ico.Reader.Export;

/// <summary>
/// Writes decoded images out to disk as PNG files.
/// </summary>
/// <remarks>
/// Available through dependency injection, but a container is not required: <c>new IcoExporter()</c>
/// is equivalent.
/// </remarks>
public interface IIcoExporter
{
    /// <summary>
    /// Saves a single image to a file.
    /// </summary>
    /// <param name="icoData">The ico data the image belongs to.</param>
    /// <param name="imageReference">The image to save.</param>
    /// <param name="path">The file path to write to. An existing file is overwritten.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    Task SaveImageAsync(IcoData icoData, ImageReference imageReference, string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves every image of one group into a directory, one PNG file per image.
    /// </summary>
    /// <param name="icoData">The ico data the group belongs to.</param>
    /// <param name="group">The group whose images are saved.</param>
    /// <param name="path">The directory to write into. It is created if it does not exist.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    Task SaveGroupToDirectoryAsync(IcoData icoData, IIcoGroup group, string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves every group into its own subdirectory, arranged by ico type and group name.
    /// </summary>
    /// <param name="icoData">The ico data to save.</param>
    /// <param name="path">The root directory to write into.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    Task SaveAllGroupsToDirectoryAsync(IcoData icoData, string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves every image into a single directory, without grouping.
    /// </summary>
    /// <param name="icoData">The ico data to save.</param>
    /// <param name="path">The directory to write into.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    Task SaveAllImagesToDirectoryAsync(IcoData icoData, string path, CancellationToken cancellationToken = default);
}
