using Ico.Reader.Creator;
using Ico.Reader.Decoder;
using Ico.Reader.Decoder.ImageDecoder;
using Ico.Reader.Decoder.ImageDecoder.Bmp;
using Ico.Reader.PeDecoder;

namespace Ico.Reader;

/// <summary>
/// The single place that knows which decoders the library ships with, so the dependency injection
/// registration and the parameterless constructors cannot drift apart.
/// </summary>
internal static class IcoReaderDefaults
{
    public static IPngCreator CreatePngCreator() => new PngCreator();

    public static IIcoBmpDecoder[] CreateBmpDecoders() =>
    [
        new IcoBmp1Decoder(),
        new IcoBmp4Decoder(),
        new IcoBmp8Decoder(),
        new IcoBmp16Decoder(),
        new IcoBmp24Decoder(),
        new IcoBmp32Decoder()
    ];

    public static IDecoder[] CreateImageDecoders() =>
    [
        new BmpDecoder(CreateBmpDecoders(), CreatePngCreator()),
        new PngDecoder()
    ];

    public static IIcoDecoder CreateIcoDecoder() => new IcoDecoder(CreateImageDecoders());

    public static IIcoPeDecoder CreateIcoPeDecoder() => new IcoPeDecoder(new PeFileDecoder());
}
