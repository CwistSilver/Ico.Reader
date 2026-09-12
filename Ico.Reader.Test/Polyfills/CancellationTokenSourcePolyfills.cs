#if NETFRAMEWORK

namespace System.Threading;

internal static class CancellationTokenSourcePolyfills
{
    public static Task CancelAsync(this CancellationTokenSource source)
    {
        source.Cancel();
        return Task.CompletedTask;
    }
}

#endif
