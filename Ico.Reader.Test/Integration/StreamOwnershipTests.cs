namespace Ico.Reader.Test.Integration;

/// <summary>
/// The reader never closes a stream it did not open. Failing to parse is reported by returning
/// null, so the caller can go on using the stream — to try another format, for example.
/// </summary>
public sealed class StreamOwnershipTests
{
    private readonly IcoReader _reader = new();

    private static MemoryStream NotAnIco() => new([0x42, 0x4D, 0x00, 0x00, 0x00, 0x00, 0x11, 0x22]);

    private static MemoryStream WithUnknownImageType()
    {
        var ico = IcoBuilder.Icon()
            .AddIcon(16, 16, 32, IcoBmpImage.TrueColor32(new (Rgb Color, byte Alpha)[16, 16]))
            .Build();
        BitConverter.GetBytes((ushort)3).CopyTo(ico, 2);

        return new MemoryStream(ico);
    }

    private static MemoryStream WithNonZeroReserved()
    {
        var ico = IcoBuilder.Icon()
            .AddIcon(16, 16, 32, IcoBmpImage.TrueColor32(new (Rgb Color, byte Alpha)[16, 16]))
            .Build();
        ico[0] = 1;

        return new MemoryStream(ico);
    }

    public static TheoryData<string> UnreadableStreams => ["not-an-ico", "unknown-image-type", "non-zero-reserved"];

    private static MemoryStream Create(string kind) => kind switch
    {
        "not-an-ico" => NotAnIco(),
        "unknown-image-type" => WithUnknownImageType(),
        "non-zero-reserved" => WithNonZeroReserved(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown stream kind.")
    };

    [Theory]
    [MemberData(nameof(UnreadableStreams))]
    public void Read_LeavesACopiedStreamOpenWhenParsingFails(string kind)
    {
        using var stream = Create(kind);

        Assert.Null(_reader.Read(stream, copyStream: true));

        Assert.True(stream.CanRead);
        Assert.Equal(stream.Length, stream.Seek(0, SeekOrigin.End));
    }

    [Theory]
    [MemberData(nameof(UnreadableStreams))]
    public void Read_LeavesADirectStreamOpenWhenParsingFails(string kind)
    {
        using var stream = Create(kind);

        Assert.Null(_reader.Read(stream, copyStream: false));

        Assert.True(stream.CanRead);
    }

    [Fact]
    public void Read_LeavesTheStreamUsableForAnotherAttempt()
    {
        // The realistic reason this matters: a caller probing an unknown file needs its stream back.
        using var stream = new MemoryStream(TestFiles.IcoBytes("icon_multi.ico"));

        var corrupted = new byte[stream.Length];
        stream.Read(corrupted, 0, corrupted.Length);
        corrupted[0] = 1;

        Assert.Null(_reader.Read(new MemoryStream(corrupted), copyStream: false));

        stream.Position = 0;
        var ico = _reader.Read(stream, copyStream: false);

        Assert.NotNull(ico);
        Assert.Equal(3, ico.ImageReferences.Count);
    }
}
