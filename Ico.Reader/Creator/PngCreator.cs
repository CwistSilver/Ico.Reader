﻿using Ico.Reader.Data;
using Ico.Reader.Utilities;
using System.IO.Compression;
using System.Text;

namespace Ico.Reader.Creator;
public class PngCreator : IPngCreator
{
    private const uint cInit = 0xffffffff;
    private const string IDAT = "IDAT";
    private const string IHDR = "IHDR";
    private const string IEND = "IEND";

    private static readonly byte[] _header = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly uint[] _crcTable = Enumerable.Range(0, 256).Select(n =>
    {
        var c = (uint)n;
        for (var k = 0; k < 8; k++)
            c = (c & 1) == 1 ? 0xedb88320 ^ c >> 1 : c >> 1;

        return c;
    }).ToArray();


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
        var bytesPerRow = width * 4;
        var uncompressedData = new byte[height * (bytesPerRow + 1)];

        for (int y = 0; y < height; y++)
        {
            var startIndex = y * (bytesPerRow + 1);
            uncompressedData[startIndex] = 0;
            rgba.Slice(y * bytesPerRow, bytesPerRow).CopyTo(uncompressedData.AsSpan(startIndex + 1));
        }

        using var compressedDataStream = new MemoryStream();
        using (var compressor = new DeflateStream(compressedDataStream, CompressionLevel.Optimal, true))
            compressor.Write(uncompressedData, 0, uncompressedData.Length);

        var compressedData = compressedDataStream.ToArray();
        WriteChunk(writer, IDAT, compressedData);
    }

    private static void WriteIendChunk(BinaryWriter writer) => WriteChunk(writer, IEND, Array.Empty<byte>());

    private static void WriteChunk(BinaryWriter writer, string type, byte[] data)
    {
        writer.WriteInt32BigEndian(data.Length);
        var typeArray = Encoding.ASCII.GetBytes(type);
        writer.Write(typeArray);
        writer.Write(data);

        var crc = CalculateCrc32(data);
        writer.WriteUInt32BigEndian(crc);
    }

    public static uint CalculateCrc32(byte[] data)
    {
        uint crc = cInit;
        foreach (var b in data)
            crc = _crcTable[(crc ^ b) & 0xff] ^ crc >> 8;

        return crc ^ cInit;
    }
}