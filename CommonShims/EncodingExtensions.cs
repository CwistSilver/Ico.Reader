using System.Text;

namespace CommonShims;

public static class EncodingExtensions
{
    public static string GetString(this Encoding encoding, ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length == 0)
            return string.Empty;

        return encoding.GetString(bytes.ToArray(), 0, bytes.Length);
    }

    public static int GetChars(this Encoding encoding, ReadOnlySpan<byte> bytes, Span<char> chars)
    {
        if (bytes.Length == 0 || chars.Length == 0)
            return 0;

        var src = bytes.ToArray();
        var dst = new char[chars.Length];

        var written = encoding.GetChars(src, 0, src.Length, dst, 0);

        dst.AsSpan(0, written).CopyTo(chars);
        return written;
    }
}
