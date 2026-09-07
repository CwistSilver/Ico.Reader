namespace Ico.Reader.Test.Integration;

/// <summary>
/// The documented contract is that every Read overload returns null for input it cannot parse.
/// <para>
/// Each case starts from <see cref="ValidIcon"/> and corrupts exactly one field, so a null result
/// can only come from the guard under test. <see cref="Read_AcceptsTheUncorruptedIcon"/> is the
/// positive control that keeps the starting point honest.
/// </para>
/// </summary>
public sealed class MalformedInputTests
{
    private readonly IcoReader _reader = new();

    private static byte[] ValidIcon() => IcoBuilder.Icon()
        .AddIcon(16, 16, 32, IcoBmpImage.TrueColor32(new (Rgb Color, byte Alpha)[16, 16]))
        .Build();

    [Fact]
    public void Read_AcceptsTheUncorruptedIcon()
    {
        var ico = _reader.Read(ValidIcon());

        Assert.NotNull(ico);
        var reference = Assert.Single(ico.ImageReferences);
        Assert.Equal(16, reference.Width);
        Assert.Equal(16, reference.Height);
        Assert.NotEmpty(ico.GetImage(0));
    }

    [Fact]
    public void Read_ReturnsNullForAMissingFile()
        => Assert.Null(_reader.Read(Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.ico")));

    [Fact]
    public void Read_ReturnsNullForAReservedFieldThatIsNotZero()
    {
        var ico = ValidIcon();
        ico[0] = 1;

        Assert.Null(_reader.Read(ico));
    }

    [Fact]
    public void Read_ReturnsNullForAnUnknownImageType()
    {
        var ico = ValidIcon();
        BitConverter.GetBytes((ushort)3).CopyTo(ico, 2);

        Assert.Null(_reader.Read(ico));
    }

    [Fact]
    public void Read_ReturnsNullWhenADirectoryEntryReservesANonZeroByte()
    {
        var ico = IcoBuilder.Icon()
            .AddIcon(16, 16, 32, IcoBmpImage.TrueColor32(new (Rgb Color, byte Alpha)[16, 16]), reserved: 1)
            .Build();

        Assert.Null(_reader.Read(ico));
    }

    [Fact]
    public void Read_ReturnsNullForAnEntryInAnUnknownImageFormat()
    {
        var garbage = new byte[64];
        garbage[0] = 0xFF;
        var ico = IcoBuilder.Icon().AddIcon(16, 16, 32, garbage).Build();

        Assert.Null(_reader.Read(ico));
    }

    [Fact]
    public void Read_ReturnsNullForBytesThatAreNotAnIcoAtAll()
        => Assert.Null(_reader.Read(new byte[] { 0x42, 0x4D, 0x00, 0x00, 0x00, 0x00, 0x11, 0x22 }));

    [Fact]
    public void Read_ReturnsNullForAnMzHeaderWithoutAPeHeader()
    {
        var data = new byte[128];
        data[0] = (byte)'M';
        data[1] = (byte)'Z';

        Assert.Null(_reader.Read(data));
    }

    [Fact]
    public void Read_HandlesAnEmptyDirectory()
    {
        var ico = IcoBuilder.Icon().Build();

        var result = _reader.Read(ico);

        Assert.NotNull(result);
        Assert.Empty(result.ImageReferences);
        Assert.Equal(-1, result.PreferredImageIndex());
    }

    [Fact]
    public void Read_ReturnsNullForAnEmptyFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"empty-{Guid.NewGuid():N}.ico");
        File.WriteAllBytes(path, []);
        try
        {
            Assert.Null(_reader.Read(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GetImage_ThrowsWhenTheImageDataIsTruncated()
    {
        var ico = ValidIcon();
        var truncated = ico.AsSpan(0, ico.Length - 32).ToArray();

        var result = _reader.Read(truncated);

        Assert.NotNull(result);
        Assert.Throws<EndOfStreamException>(() => result.GetImage(0));
    }

    [Fact]
    public async Task GetImageAsync_ThrowsWhenTheImageDataIsTruncated()
    {
        var ico = ValidIcon();
        var truncated = ico.AsSpan(0, ico.Length - 32).ToArray();

        var result = _reader.Read(truncated);

        Assert.NotNull(result);
        await Assert.ThrowsAsync<EndOfStreamException>(() => result.GetImageAsync(0));
    }
}
