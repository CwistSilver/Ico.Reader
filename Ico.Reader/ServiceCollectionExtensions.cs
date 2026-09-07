using Ico.Reader.Creator;
using Ico.Reader.Data;
using Ico.Reader.Decoder;
using Ico.Reader.Decoder.ImageDecoder;
using Ico.Reader.Decoder.ImageDecoder.Bmp;
using Ico.Reader.PeDecoder;

using Microsoft.Extensions.DependencyInjection;

namespace Ico.Reader;

/// <summary>
/// Contains extension methods for configuring ico reading services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds services necessary for reading and decoding ico files.
    /// </summary>
    /// <remarks>
    /// Using a container is optional. Every type registered here is also reachable through a
    /// parameterless constructor, so <c>new IcoReader()</c> produces the same composition.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddIcoReader(this IServiceCollection services)
    {
        // The default set comes from IcoReaderDefaults so this list and the parameterless
        // constructors cannot disagree about which decoders ship.
        foreach (var bmpDecoder in IcoReaderDefaults.CreateBmpDecoders())
        {
            services.AddSingleton(bmpDecoder);
        }

        services.AddSingleton<IPngCreator>(_ => IcoReaderDefaults.CreatePngCreator());

        // Composed from the container rather than from the defaults, so a caller that registers an
        // extra IIcoBmpDecoder gets it picked up.
        services.AddSingleton<IDecoder>(p => new BmpDecoder(p.GetServices<IIcoBmpDecoder>(), p.GetRequiredService<IPngCreator>()));
        services.AddSingleton<IDecoder, PngDecoder>();

        services.AddSingleton<IPeDecoder, PeFileDecoder>();
        services.AddSingleton<IIcoDecoder, IcoDecoder>();
        services.AddSingleton<IIcoPeDecoder, IcoPeDecoder>();
        services.AddSingleton<Export.IIcoExporter, Export.IcoExporter>();

        services.AddSingleton(p => new IcoReader(new IcoReaderConfiguration
        {
            IcoExeDecoder = p.GetRequiredService<IIcoPeDecoder>(),
            IcoDecoder = p.GetRequiredService<IIcoDecoder>()
        }));

        return services;
    }
}
