using Ico.Reader.Data;

namespace Ico.Reader.Creator;

public interface IPngCreator
{
    byte[] CreatePng(ReadOnlySpan<byte> rgba, BMP_Info_Header header);
}