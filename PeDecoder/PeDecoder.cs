using PeDecoder.Models;

namespace PeDecoder;

public interface IPeDecoder
{
    MZ_Header DecodeMZ(Stream stream);
    PE_Header DecodePE(Stream stream);
    ResourceDirectory? DecodeResourceDirectory(Stream stream, PE_Header peHeader);
    bool IsPeFormat(MZ_Header mZ_Header);
    bool IsPeFormat(Stream stream);
}

public class PeDecoder : IPeDecoder
{
    public MZ_Header DecodeMZ(Stream stream) => MZ_Header.ReadFromStream(stream);
    public bool IsPeFormat(Stream stream) => IsPeFormat(MZ_Header.ReadFromStream(stream));
    public bool IsPeFormat(MZ_Header mZ_Header)
    {
        if (mZ_Header.Signature[0] != 'M' || mZ_Header.Signature[1] != 'Z')
            return false;

        return true;
    }

    public PE_Header DecodePE(Stream stream) => PE_Header.ReadFromStream(stream);
    public ResourceDirectory? DecodeResourceDirectory(Stream stream, PE_Header peHeader) => ResourceDirectory.ReadFromStream(stream, peHeader);
}

