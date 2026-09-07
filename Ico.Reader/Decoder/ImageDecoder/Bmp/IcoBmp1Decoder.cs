namespace Ico.Reader.Decoder.ImageDecoder.Bmp;

/// <summary>
/// Decodes 1 bit per pixel indexed BMP icon data, where each bit selects one of two palette entries.
/// </summary>
public sealed class IcoBmp1Decoder : IndexedBmpDecoder
{
    /// <inheritdoc/>
    public override byte BitCountSupported => 1;

    /// <inheritdoc/>
    protected override byte ReadPaletteIndex(ReadOnlySpan<byte> row, int x)
        => (byte)((row[x / 8] >> (7 - (x % 8))) & 1);
}
