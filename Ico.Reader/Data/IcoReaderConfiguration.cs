using Ico.Reader.Decoder;

namespace Ico.Reader.Data;

public sealed class IcoReaderConfiguration
{
    /// <summary>
    /// The decoder used for standalone ICO and CUR files.
    /// </summary>
    public IIcoDecoder IcoDecoder { get; init; } = IcoReaderDefaults.CreateIcoDecoder();

    /// <summary>
    /// The decoder used for icons and cursors embedded in EXE and DLL files.
    /// </summary>
    public IIcoPeDecoder IcoExeDecoder { get; init; } = IcoReaderDefaults.CreateIcoPeDecoder();
}
