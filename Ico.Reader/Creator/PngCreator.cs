using System.IO.Compression;
using System.Text;

using Ico.Reader.Data;
using Ico.Reader.Extensions;

namespace Ico.Reader.Creator;

public class PngCreator : IPngCreator
{
    private const uint cInit = 0xffffffff;
    private const string IDAT = "IDAT";
    private const string IHDR = "IHDR";
    private const string IEND = "IEND";

    private static readonly byte[] _header = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly uint[] _crcTable = [.. Enumerable.Range(0, 256).Select(n =>
    {
        var c = (uint)n;
        for (var k = 0; k < 8; k++)
            c = (c & 1) == 1 ? 0xedb88320 ^ (c >> 1) : c >> 1;

        return c;
    })];

    public byte[] CreatePng(ReadOnlySpan<byte> rgba, BMP_Info_Header header)
    {
        var width = header.Width;
        var height = header.Height / 2;

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(_header);
        WriteIhdrChunk(writer, width, height);
        WriteIdatChunks(writer, rgba, width, height);
        WriteIendChunk(writer);

        return ms.ToArray();
    }

    private static void WriteIhdrChunk(BinaryWriter writer, int width, int height)
    {
        using var chunkStream = new MemoryStream();
        using var chunkWriter = new BinaryWriter(chunkStream);
        chunkWriter.WriteInt32BigEndian(width);
        chunkWriter.WriteInt32BigEndian(height);
        chunkWriter.Write((byte)8);
        chunkWriter.Write((byte)6);
        chunkWriter.Write((byte)0);
        chunkWriter.Write((byte)0);
        chunkWriter.Write((byte)0);

        WriteChunk(writer, IHDR, chunkStream.ToArray());
    }

    private static void WriteIdatChunks(BinaryWriter writer, ReadOnlySpan<byte> rgba, int width, int height)
    {
        var bytesPerRow = width * 4 + 1;
        var uncompressedData = new byte[height * bytesPerRow];

        for (var y = 0; y < height; y++)
        {
            uncompressedData[y * bytesPerRow] = 0;
            rgba.Slice(y * width * 4, width * 4).CopyTo(uncompressedData.AsSpan(y * bytesPerRow + 1));
        }

        using var compressedDataStream = new MemoryStream();
        compressedDataStream.WriteByte(0x78);
        compressedDataStream.WriteByte(0x9C);

        using var compressor = new DeflateStream(compressedDataStream, CompressionLevel.Optimal, true);
        compressor.Write(uncompressedData, 0, uncompressedData.Length);
        compressor.Flush();
        compressor.Close();

        var adler = CalculateAdler32(uncompressedData);
        using var binaryWriter = new BinaryWriter(compressedDataStream);
        binaryWriter.WriteUInt32BigEndian(adler);

        var compressedData = compressedDataStream.ToArray();
        WriteChunk(writer, IDAT, compressedData);
    }

    private static void WriteIendChunk(BinaryWriter writer) => WriteChunk(writer, IEND, Array.Empty<byte>());

    private static void WriteChunk(BinaryWriter writer, string type, byte[] data)
    {
        var typeArray = Encoding.ASCII.GetBytes(type);
        writer.WriteInt32BigEndian(data.Length);
        writer.Write(typeArray);
        writer.Write(data);

        var crcInput = new byte[typeArray.Length + data.Length];
        typeArray.CopyTo(crcInput, 0);
        data.CopyTo(crcInput, typeArray.Length);
        var crc = CalculateCrc32(crcInput);

        writer.WriteUInt32BigEndian(crc);
    }

    public static uint CalculateCrc32(byte[] data)
    {
        var crc = cInit;
        foreach (var b in data)
            crc = _crcTable[(crc ^ b) & 0xff] ^ (crc >> 8);

        return crc ^ cInit;
    }

    private static uint CalculateAdler32(byte[] data)
    {
        const uint MOD_ADLER = 65521;
        uint a = 1, b = 0;

        foreach (var byteValue in data)
        {
            a = (a + byteValue) % MOD_ADLER;
            b = (b + a) % MOD_ADLER;
        }

        return (b << 16) | a;
    }
}