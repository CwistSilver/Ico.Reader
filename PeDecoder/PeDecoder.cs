using PeDecoder.Models;

namespace PeDecoder;

internal interface IPeDecoder
{
    MzHeader DecodeMZ(Stream stream);
    PeHeader DecodePE(Stream stream);
    ResourceDirectory? DecodeResourceDirectory(Stream stream, PeHeader peHeader);
    bool IsPeFormat(MzHeader mzHeader);
    bool IsPeFormat(Stream stream);
}

internal sealed class PeDecoder : IPeDecoder
{
    public MzHeader DecodeMZ(Stream stream) => MzHeader.ReadFromStream(stream);

    public bool IsPeFormat(Stream stream) => IsPeFormat(MzHeader.ReadFromStream(stream));

    public bool IsPeFormat(MzHeader mzHeader) => mzHeader.HasMzSignature;

    public PeHeader DecodePE(Stream stream) => PeHeader.ReadFromStream(stream);
    public ResourceDirectory? DecodeResourceDirectory(Stream stream, PeHeader peHeader) => ResourceDirectory.ReadFromStream(stream, peHeader);
}

