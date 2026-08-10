using System.Buffers;

namespace Ico.Reader.Utils;

internal delegate T EntryParser<out T>(ReadOnlySpan<byte> entry);

internal static class DirectoryEntryReader
{
    /// <summary>
    /// Reads a contiguous run of fixed-size directory entries from a stream and parses each one.
    /// </summary>
    /// <remarks>
    /// The entry count comes from untrusted file data, so the scratch buffer is pooled rather than
    /// stack-allocated.
    /// </remarks>
    public static T[] ReadEntries<T>(Stream stream, int entrySize, int entryCount, EntryParser<T> parseEntry)
    {
        if (entryCount <= 0)
            return Array.Empty<T>();

        var entries = new T[entryCount];
        var byteSize = entrySize * entryCount;
        var buffer = ArrayPool<byte>.Shared.Rent(byteSize);
        try
        {
            stream.Read(buffer, 0, byteSize);

            for (var i = 0; i < entryCount; i++)
                entries[i] = parseEntry(new ReadOnlySpan<byte>(buffer, i * entrySize, entrySize));

            return entries;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
