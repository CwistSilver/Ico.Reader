namespace Ico.Reader.Data;

/// <summary>
/// Ranks images by pixel area and colour bit depth.
/// </summary>
internal static class ImageQuality
{
    /// <summary>
    /// Scores each image as a weighted sum of its pixel area and colour bit depth, both expressed as
    /// a fraction of the largest value present, and returns the index of the highest scoring image.
    /// <para>
    /// The weights are a ratio and are normalised internally, so 2 and 1 rank identically to 0.667
    /// and 0.333. Because both terms are relative to the supplied set, a score is only meaningful
    /// within that set.
    /// </para>
    /// </summary>
    /// <returns>The index of the best image, or -1 when there are none.</returns>
    public static int BestIndex(IReadOnlyList<ImageReference> imageReferences, float colorBitWeight, float areaWeight)
    {
        if (colorBitWeight < 0)
            throw new ArgumentOutOfRangeException(nameof(colorBitWeight), colorBitWeight, "Weights cannot be negative.");
        if (areaWeight < 0)
            throw new ArgumentOutOfRangeException(nameof(areaWeight), areaWeight, "Weights cannot be negative.");

        var weightSum = colorBitWeight + areaWeight;
        if (weightSum <= 0)
            throw new ArgumentException("At least one weight must be greater than zero.", nameof(areaWeight));

        if (imageReferences.Count == 0)
            return -1;

        long maxArea = 0;
        var maxBitCount = 0;
        foreach (var imageReference in imageReferences)
        {
            maxArea = Math.Max(maxArea, Area(imageReference));
            maxBitCount = Math.Max(maxBitCount, imageReference.BitCount);
        }

        var bestIndex = 0;
        var bestQuality = double.NegativeInfinity;

        for (var i = 0; i < imageReferences.Count; i++)
        {
            var imageReference = imageReferences[i];
            var areaRatio = maxArea > 0 ? (double)Area(imageReference) / maxArea : 0;
            var bitRatio = maxBitCount > 0 ? (double)imageReference.BitCount / maxBitCount : 0;
            var qualityScore = ((areaWeight * areaRatio) + (colorBitWeight * bitRatio)) / weightSum;

            if (qualityScore > bestQuality)
            {
                bestQuality = qualityScore;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private static long Area(ImageReference imageReference) => (long)imageReference.Width * imageReference.Height;
}
