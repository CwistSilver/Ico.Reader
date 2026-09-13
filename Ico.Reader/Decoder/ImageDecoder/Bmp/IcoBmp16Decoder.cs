using System.Runtime.InteropServices;

using Ico.Reader.Data;

namespace Ico.Reader.Decoder.ImageDecoder.Bmp;

/// <summary>
/// Decodes 16 bit per pixel BMP icon data, where each pixel packs five bits per colour channel and transparency comes
/// from the AND mask.
/// </summary>
public sealed class IcoBmp16Decoder : IIcoBmpDecoder
{
    private const int BytesPerPixel = 2;
    private const int ChannelMask = 0x1F;

    /// <inheritdoc/>
    public byte BitCountSupported => 16;

    /// <inheritdoc/>
    public byte[] DecodeIcoBmpToRgba(ReadOnlySpan<byte> data, BmpInfoHeader header)
    {
        var width = header.Width;
        var height = header.Height / 2;
        var rgbaData = new byte[width * height * 4];

        var dataOffset = header.CalculateDataOffset();
        var pixelStride = BmpLayout.Stride(width * 16);
        var maskStride = BmpLayout.MaskStride(width);
        var maskOffset = dataOffset + (pixelStride * height);
        var hasMask = BmpLayout.HasMask(data, maskOffset, width, height);

        for (var y = 0; y < height; y++)
        {
            var rowOffset = dataOffset + (y * pixelStride);

            // ICO stores rows bottom-up, so the last row read is the top row of the image.
            var targetRow = height - 1 - y;

            for (var x = 0; x < width; x++)
            {
                var pixel = MemoryMarshal.Read<ushort>(data.Slice(rowOffset + (x * BytesPerPixel), BytesPerPixel));
                var pixelIndex = ((targetRow * width) + x) * 4;

                rgbaData[pixelIndex] = ExpandChannel(pixel >> 10);
                rgbaData[pixelIndex + 1] = ExpandChannel(pixel >> 5);
                rgbaData[pixelIndex + 2] = ExpandChannel(pixel);

                if (!hasMask || !BmpLayout.IsTransparent(data, maskOffset, maskStride, y, x))
                    rgbaData[pixelIndex + 3] = 255;
            }
        }

        return rgbaData;
    }

    /// <summary>
    /// Widens a five bit channel to eight bits by repeating its top bits in the low ones, which maps 31 to 255 and gives
    /// the colours Windows draws.
    /// </summary>
    private static byte ExpandChannel(int packed)
    {
        var value = packed & ChannelMask;
        return (byte)((value << 3) | (value >> 2));
    }
}
