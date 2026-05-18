using Ico.Reader.Data;

namespace Ico.Reader.Decoder.ImageDecoder.Bmp;

public sealed class IcoBmp24Decoder : IIcoBmpDecoder
{
    public byte BitCountSupported => 24;

    public byte[] DecodeIcoBmpToRgba(ReadOnlySpan<byte> data, BMP_Info_Header header)
    {
        var width = header.Width;
        var height = header.Height / 2;
        var argbData = new byte[width * height * 4];

        var dataOffset = header.Size + (header.ClrUsed * 4);
        var bytesPerRowImage = 3 * width;
        var imageRowPadding = (4 - (bytesPerRowImage % 4)) % 4;
        var totalImageSize = (bytesPerRowImage + imageRowPadding) * height;

        var maskOffset = dataOffset + totalImageSize;

        var maskRowBytesActual = (width + 7) / 8;
        var maskPadding = (4 - (maskRowBytesActual % 4)) % 4;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixelIndex = (((height - 1 - y) * width) + x) * 4;
                var dataRowOffset = dataOffset + (y * (bytesPerRowImage + imageRowPadding)) + (x * 3);

                argbData[pixelIndex] = data[dataRowOffset + 2];
                argbData[pixelIndex + 1] = data[dataRowOffset + 1];
                argbData[pixelIndex + 2] = data[dataRowOffset];

                var maskByteIndex = maskOffset + (y * (maskRowBytesActual + maskPadding)) + (x / 8);
                var maskBit = 7 - (x % 8);
                var isTransparent = ((data[maskByteIndex] >> maskBit) & 1) == 1;

                if (!isTransparent)
                    argbData[pixelIndex + 3] = 255;
            }
        }

        return argbData;
    }
}
