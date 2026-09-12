#if NETFRAMEWORK

namespace System.IO;

internal static class StreamPolyfills
{
    public static void Write(this Stream stream, ReadOnlySpan<byte> buffer)
        => stream.Write(buffer.ToArray(), 0, buffer.Length);
}

#endif
