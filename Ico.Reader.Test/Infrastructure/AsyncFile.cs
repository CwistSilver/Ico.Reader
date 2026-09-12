namespace Ico.Reader.Test.Infrastructure;

/// <summary>
/// Asynchronous file helpers. <see cref="File"/> is a static class, so the members .NET Framework
/// is missing cannot be added to it the way the other polyfills extend their types.
/// </summary>
internal static class AsyncFile
{
    public static Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken)
    {
#if NETFRAMEWORK
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(File.ReadAllBytes(path));
#else
        return File.ReadAllBytesAsync(path, cancellationToken);
#endif
    }

    public static Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken)
    {
#if NETFRAMEWORK
        cancellationToken.ThrowIfCancellationRequested();
        File.WriteAllBytes(path, bytes);
        return Task.CompletedTask;
#else
        return File.WriteAllBytesAsync(path, bytes, cancellationToken);
#endif
    }
}
