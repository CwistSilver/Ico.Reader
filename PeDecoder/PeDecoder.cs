using PeDecoder.Models;
using PeDecoder.Reading;

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
    public MzHeader DecodeMZ(Stream stream) => MzHeaderReader.Read(stream);

    public bool IsPeFormat(Stream stream) => IsPeFormat(MzHeaderReader.Read(stream));

    public bool IsPeFormat(MzHeader mzHeader) => mzHeader.HasMzSignature;

    public PeHeader DecodePE(Stream stream) => PeHeaderReader.Read(stream);
    public ResourceDirectory? DecodeResourceDirectory(Stream stream, PeHeader peHeader) => ResourceReader.Read(stream, peHeader);
}

