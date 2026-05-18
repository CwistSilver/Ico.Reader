using Ico.Reader.Creator;
using Ico.Reader.Data;
using Ico.Reader.Decoder;
using Ico.Reader.Decoder.ImageDecoder;
using Ico.Reader.Decoder.ImageDecoder.Bmp;

using Microsoft.Extensions.DependencyInjection;

using PeDecoder;

namespace Ico.Reader;
/// <summary>
/// Contains extension methods for configuring ico reading services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds services necessary for reading and decoding ico files.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddIcoReader(this IServiceCollection services)
    {
        services.AddSingleton<IIcoBmpDecoder, IcoBmp1Decoder>();
        services.AddSingleton<IIcoBmpDecoder, IcoBmp4Decoder>();
        services.AddSingleton<IIcoBmpDecoder, IcoBmp8Decoder>();
        services.AddSingleton<IIcoBmpDecoder, IcoBmp24Decoder>();
        services.AddSingleton<IIcoBmpDecoder, IcoBmp32Decoder>();
        services.AddSingleton<IDecoder, BmpDecoder>();
        services.AddSingleton<IDecoder, PngDecoder>();

        services.AddSingleton<IPeDecoder, PeDecoder.PeDecoder>();
        services.AddSingleton<IIcoDecoder, IcoDecoder>();
        services.AddSingleton<IIcoPeDecoder, IcoPeDecoder>();

        services.AddSingleton<IPngCreator, PngCreator>();

        services.AddSingleton(p =>
        {
            var configuration = new IcoReaderConfiguration
            {
                IcoExeDecoder = p.GetRequiredService<IIcoPeDecoder>(),
                IcoDecoder = p.GetRequiredService<IIcoDecoder>()
            };

            return new IcoReader(configuration);
        });

        return services;
    }
}
