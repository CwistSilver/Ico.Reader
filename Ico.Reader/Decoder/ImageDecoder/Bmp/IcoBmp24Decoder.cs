using Ico.Reader.Data;

namespace Ico.Reader.Decoder.ImageDecoder.Bmp;
public sealed class IcoBmp24Decoder : IIcoBmpDecoder
{
    public byte BitCountSupported => 24;

    public byte[] DecodeIcoBmpToRgba(ReadOnlySpan<byte> data, BMP_Info_Header header)
    {
        int width = header.Width;
        int height = header.Height / 2;
        byte[] argbData = new byte[width * height * 4];

        int dataOffset = header.Size + header.ClrUsed * 4;
        int bytesPerRowImage = 3 * width;
        int imageRowPadding = (4 - (bytesPerRowImage % 4)) % 4;
        int totalImageSize = (bytesPerRowImage + imageRowPadding) * height;

        int maskOffset = dataOffset + totalImageSize;

        int maskRowBytesActual = (width + 7) / 8;
        int maskPadding = (4 - (maskRowBytesActual % 4)) % 4;


        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int pixelIndex = ((height - 1 - y) * width + x) * 4;
                int dataRowOffset = dataOffset + y * (bytesPerRowImage + imageRowPadding) + x * 3;

                argbData[pixelIndex] = data[dataRowOffset + 2];
                argbData[pixelIndex + 1] = data[dataRowOffset + 1];
                argbData[pixelIndex + 2] = data[dataRowOffset];

                int maskByteIndex = maskOffset + y * (maskRowBytesActual + maskPadding) + x / 8;
                int maskBit = 7 - (x % 8);
                bool isTransparent = ((data[maskByteIndex] >> maskBit) & 1) == 1;

                if (!isTransparent)
                    argbData[pixelIndex + 3] = 255;
            }
        }

        return argbData;
    }
}
