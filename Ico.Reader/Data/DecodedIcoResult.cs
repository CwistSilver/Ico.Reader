namespace Ico.Reader.Data;
/// <summary>
/// Represents the result of decoding an ico file.
/// </summary>
public sealed class DecodedIcoResult
{
    /// <summary>
    /// The origin file type of the ico file.
    /// </summary>
    public IcoOriginFileType OriginFileType { get; set; }

    /// <summary>
    /// An array of image references, each pointing to an image extracted from the ico file.
    /// </summary>
    public List<ImageReference> References { get; set; } = [];
    /// <summary>
    /// An array of ico groups, categorizing the extracted images into groups based on certain criteria, such as resolution or color depth.
    /// </summary>
    public List<IIcoGroup> IcoGroups { get; set; } = [];
}
