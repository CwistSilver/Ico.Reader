namespace Ico.Reader.Decoder.ImageDecoder.Bmp;

/// <summary>
/// Decodes 8 bit per pixel indexed BMP icon data, where each byte selects a palette entry.
/// </summary>
public sealed class IcoBmp8Decoder : IndexedBmpDecoder
{
    /// <inheritdoc/>
    public override byte BitCountSupported => 8;

    /// <inheritdoc/>
    protected override byte ReadPaletteIndex(ReadOnlySpan<byte> row, int x)
        => row[x];
}
