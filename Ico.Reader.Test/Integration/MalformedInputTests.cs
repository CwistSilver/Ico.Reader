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
    public void Read_IgnoresTheReservedByteOfADirectoryEntry()
    {
        // Windows loads such a file, so the byte is not a reason to reject it.
        var ico = IcoBuilder.Icon()
            .AddIcon(16, 16, 32, IcoBmpImage.TrueColor32(new (Rgb Color, byte Alpha)[16, 16]), reserved: 255)
            .Build();

        var result = _reader.Read(ico);

        Assert.NotNull(result);
        Assert.NotEmpty(result.GetImage(Assert.Single(result.ImageReferences)));
    }

    [Fact]
    public void Read_SkipsAnEntryInAnUnknownImageFormatAndKeepsTheOthers()
    {
        // Windows still loads the other images of such a file, as Ico.Reader already did for EXE and DLL files.
        var garbage = new byte[64];
        garbage[0] = 0xFF;
        var ico = IcoBuilder.Icon()
            .AddIcon(16, 16, 32, garbage)
            .AddIcon(32, 32, 32, IcoBmpImage.TrueColor32(new (Rgb Color, byte Alpha)[32, 32]))
            .Build();

        var result = _reader.Read(ico);

        Assert.NotNull(result);
        var reference = Assert.Single(result.ImageReferences);
        Assert.Equal(1, reference.Id);
        Assert.Equal(32, reference.Width);

        var group = Assert.Single(result.Groups);
        Assert.Equal(1, group.Size);
        Assert.Equal(32, PngImage.Parse(result.GetImage(group, 0)).Width);
    }

    [Fact]
    public void Read_ReturnsNullWhenNoEntryHoldsAReadableImage()
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
    public void GetImage_ChecksTheDeclaredImageSizeBeforeAllocatingIt()
    {
        var ico = ValidIcon();
        BitConverter.GetBytes(256u * 1024 * 1024).CopyTo(ico, FirstEntryImageSizeField);
        var result = _reader.Read(ico);
        Assert.NotNull(result);

        var allocated = AllocatedBytes(() => Assert.Throws<EndOfStreamException>(() => result.GetImage(0)));

        Assert.True(allocated < AllocationLimit, $"Allocated {allocated} bytes before failing.");
    }

    [Fact]
    public void GetImage_ChecksTheBitmapDimensionsBeforeAllocatingPixels()
    {
        // A 56 byte bitmap claiming 8192x8192 pixels would need 256 MB of RGBA for pixels it cannot hold.
        var image = new byte[56];
        BitConverter.GetBytes(40).CopyTo(image, 0);
        BitConverter.GetBytes(8192).CopyTo(image, 4);
        BitConverter.GetBytes(8192 * 2).CopyTo(image, 8);
        BitConverter.GetBytes((ushort)1).CopyTo(image, 12);
        BitConverter.GetBytes((ushort)32).CopyTo(image, 14);
        var result = _reader.Read(IcoBuilder.Icon().AddIcon(0, 0, 32, image).Build());
        Assert.NotNull(result);

        var allocated = AllocatedBytes(() => Assert.Throws<EndOfStreamException>(() => result.GetImage(0)));

        Assert.True(allocated < AllocationLimit, $"Allocated {allocated} bytes before failing.");
    }

    [Theory]
    [InlineData(0, 32)]
    [InlineData(-16, 32)]
    [InlineData(16, 0)]
    [InlineData(16, -32)]
    public void GetImage_RejectsBitmapDimensionsThatCannotBeDecoded(int width, int stackedHeight)
    {
        var image = IcoBmpImage.TrueColor32(new (Rgb Color, byte Alpha)[16, 16]);
        BitConverter.GetBytes(width).CopyTo(image, 4);
        BitConverter.GetBytes(stackedHeight).CopyTo(image, 8);
        var result = _reader.Read(IcoBuilder.Icon().AddIcon(16, 16, 32, image).Build());
        Assert.NotNull(result);

        Assert.Throws<InvalidDataException>(() => result.GetImage(0));
    }

    private const int FirstEntryImageSizeField = 6 + 8;
    private const long AllocationLimit = 16 * 1024 * 1024;

    private static long AllocatedBytes(Action action)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        action();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact]
    public async Task GetImageAsync_ThrowsWhenTheImageDataIsTruncated()
    {
        var ico = ValidIcon();
        var truncated = ico.AsSpan(0, ico.Length - 32).ToArray();

        var result = _reader.Read(truncated);

        Assert.NotNull(result);
        await Assert.ThrowsAsync<EndOfStreamException>(() => result.GetImageAsync(0, TestContext.Current.CancellationToken));
    }
}
