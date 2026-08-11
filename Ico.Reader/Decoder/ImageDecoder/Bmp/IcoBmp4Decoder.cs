namespace Ico.Reader.Decoder.ImageDecoder.Bmp;

public sealed class IcoBmp4Decoder : IndexedBmpDecoder
{
    /// <inheritdoc/>
    public override byte BitCountSupported => 4;

    /// <inheritdoc/>
    protected override byte ReadPaletteIndex(ReadOnlySpan<byte> row, int x)
    {
        var packed = row[x / 2];
        return x % 2 == 0 ? (byte)(packed >> 4) : (byte)(packed & 0x0F);
    }
}
