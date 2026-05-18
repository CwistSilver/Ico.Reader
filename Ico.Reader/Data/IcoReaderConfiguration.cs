using Ico.Reader.Decoder;

namespace Ico.Reader.Data;

public sealed class IcoReaderConfiguration
{
    public IIcoDecoder IcoDecoder { get; set; } = new IcoDecoder();
    public IIcoPeDecoder IcoExeDecoder { get; set; } = new IcoPeDecoder(new PeDecoder.PeDecoder());
}
