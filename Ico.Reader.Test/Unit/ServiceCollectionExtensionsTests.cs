using Ico.Reader.Creator;
using Ico.Reader.Decoder;
using Ico.Reader.Decoder.ImageDecoder;
using Ico.Reader.Decoder.ImageDecoder.Bmp;

using Microsoft.Extensions.DependencyInjection;

using PeDecoder;

namespace Ico.Reader.Test.Unit;

public sealed class ServiceCollectionExtensionsTests
{
    private static ServiceProvider BuildProvider() => new ServiceCollection().AddIcoReader().BuildServiceProvider();

    [Fact]
    public void AddIcoReader_ResolvesTheReader()
    {
        using var provider = BuildProvider();

        Assert.NotNull(provider.GetRequiredService<IcoReader>());
    }

    [Fact]
    public void AddIcoReader_RegistersEveryBmpBitDepth()
    {
        using var provider = BuildProvider();

        var decoders = provider.GetServices<IIcoBmpDecoder>().ToArray();

        Assert.Equal([1, 4, 8, 24, 32], decoders.Select(x => (int)x.BitCountSupported).OrderBy(x => x));
    }

    [Fact]
    public void AddIcoReader_RegistersBothImageFormats()
    {
        using var provider = BuildProvider();

        var formats = provider.GetServices<IDecoder>().Select(x => x.SupportedFormat).ToArray();

        Assert.Contains(IcoImageFormat.BMP, formats);
        Assert.Contains(IcoImageFormat.PNG, formats);
    }

    [Fact]
    public void AddIcoReader_RegistersTheDecodersAsSingletons()
    {
        using var provider = BuildProvider();

        Assert.Same(provider.GetRequiredService<IIcoDecoder>(), provider.GetRequiredService<IIcoDecoder>());
        Assert.Same(provider.GetRequiredService<IIcoPeDecoder>(), provider.GetRequiredService<IIcoPeDecoder>());
        Assert.Same(provider.GetRequiredService<IPeDecoder>(), provider.GetRequiredService<IPeDecoder>());
        Assert.Same(provider.GetRequiredService<IPngCreator>(), provider.GetRequiredService<IPngCreator>());
    }

    [Fact]
    public void AddIcoReader_ReadsAFixtureThroughTheContainer()
    {
        using var provider = BuildProvider();
        var reader = provider.GetRequiredService<IcoReader>();

        var ico = reader.Read(TestFiles.Ico("icon_32_32bpp.ico"));

        Assert.NotNull(ico);
        Assert.Single(ico.ImageReferences);
    }

    [Fact]
    public void AddIcoReader_ReturnsTheServiceCollectionForChaining()
    {
        var services = new ServiceCollection();

        Assert.Same(services, services.AddIcoReader());
    }
}
