using Ico.Reader.Data;

namespace Ico.Reader.Creator;

public interface IPngCreator
{
    byte[] CreatePng(ReadOnlySpan<byte> rgba, BmpInfoHeader header);
}