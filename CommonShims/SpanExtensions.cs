using System.Runtime.InteropServices;

namespace CommonShims;

public static class SpanExtensions
{
    public static unsafe string ToStringFast(this ReadOnlySpan<char> span)
    {
        if (span.Length == 0)
            return string.Empty;

        fixed (char* p = &MemoryMarshal.GetReference(span))
        {
            return new string(p, 0, span.Length);
        }
    }
}
