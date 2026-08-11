namespace Ico.Reader.Decoder.ImageDecoder.Bmp;

public sealed class IcoBmp8Decoder : IndexedBmpDecoder
{
    /// <inheritdoc/>
    public override byte BitCountSupported => 8;

    /// <inheritdoc/>
    protected override byte ReadPaletteIndex(ReadOnlySpan<byte> row, int x)
        => row[x];
}
