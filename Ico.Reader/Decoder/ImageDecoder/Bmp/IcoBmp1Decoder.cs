namespace Ico.Reader.Decoder.ImageDecoder.Bmp;

public sealed class IcoBmp1Decoder : IndexedBmpDecoder
{
    /// <inheritdoc/>
    public override byte BitCountSupported => 1;

    /// <inheritdoc/>
    protected override byte ReadPaletteIndex(ReadOnlySpan<byte> row, int x)
        => (byte)((row[x / 8] >> (7 - (x % 8))) & 1);
}
