using Ico.Reader.Data.Source;

namespace Ico.Reader.Test.Unit.Data;

public sealed class DataSourceTests
{
    private static byte[] Payload => [1, 2, 3, 4, 5];

    private static byte[] Drain(Stream stream)
    {
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    [Fact]
    public void MemorySource_ReturnsAReadOnlyStreamOverTheData()
    {
        var source = new MemorySource(Payload);

        using var stream = source.GetStream();

        Assert.False(stream.CanWrite);
        Assert.Equal(Payload, Drain(stream));
    }

    [Fact]
    public void MemorySource_HandsOutIndependentStreams()
    {
        var source = new MemorySource(Payload);

        using var first = source.GetStream();
        using var second = source.GetStream();
        first.Position = 3;

        Assert.Equal(0, second.Position);
    }

    [Fact]
    public void StreamBufferSource_HandsOutIndependentStreams()
    {
        using var origin = new MemoryStream(Payload);
        var source = new StreamBufferSource(origin);

        using var first = source.GetStream();
        using var second = source.GetStream();
        first.Position = 3;

        Assert.Equal(0, second.Position);
    }

    [Fact]
    public void PathSource_HandsOutIndependentStreams()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, Payload);
            var source = new PathSource(path);

            using var first = source.GetStream();
            using var second = source.GetStream();
            first.Position = 3;

            Assert.Equal(0, second.Position);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void StreamSource_SharesOnePositionAcrossEveryStreamItHandsOut()
    {
        // Unlike the other sources, this one wraps the caller's stream rather than opening a new
        // one, so every reader shares a position. That is why a non-copied stream is single-reader.
        using var origin = new MemoryStream(Payload);
        var source = new StreamSource(origin);

        using var first = source.GetStream();
        using var second = source.GetStream();
        first.Position = 3;

        Assert.Equal(3, second.Position);
    }

    [Fact]
    public void PathSource_ReadsTheFile()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, Payload);
            var source = new PathSource(path);

            using var stream = source.GetStream();

            Assert.Equal(Payload, Drain(stream));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void PathSource_RejectsABlankPath(string path) => Assert.Throws<ArgumentException>(() => new PathSource(path));

    [Fact]
    public void PathSource_RejectsAMissingFile()
        => Assert.Throws<FileNotFoundException>(() => new PathSource(Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid()}.ico")));

    [Fact]
    public void StreamBufferSource_SurvivesTheOriginStreamBeingDisposed()
    {
        StreamBufferSource source;
        using (var origin = new MemoryStream(Payload))
            source = new StreamBufferSource(origin);

        using var stream = source.GetStream();

        Assert.Equal(Payload, Drain(stream));
    }

    [Fact]
    public void StreamBufferSource_RestoresTheOriginPosition()
    {
        using var origin = new MemoryStream(Payload) { Position = 2 };

        _ = new StreamBufferSource(origin);

        Assert.Equal(2, origin.Position);
    }

    [Fact]
    public void StreamBufferSource_CopiesFromTheCurrentPosition()
    {
        using var origin = new MemoryStream(Payload) { Position = 2 };

        var source = new StreamBufferSource(origin);

        using var stream = source.GetStream();
        Assert.Equal([3, 4, 5], Drain(stream));
    }

    [Fact]
    public void StreamBufferSource_CopiesAStreamThatCannotSeek()
    {
        using var origin = new UnseekableStream(Payload);

        var source = new StreamBufferSource(origin);

        using var stream = source.GetStream();
        Assert.Equal(Payload, Drain(stream));
    }

    [Fact]
    public void StreamSource_CountsPositionsFromWhereTheStreamWasHandedOver()
    {
        using var origin = new MemoryStream(Payload) { Position = 2 };
        var source = new StreamSource(origin);
        origin.Position = 4;

        using var stream = source.GetStream();
        stream.Position = 0;

        Assert.Equal(3, stream.Length);
        Assert.Equal(2, origin.Position);
        Assert.Equal([3, 4, 5], Drain(stream));
    }

    [Fact]
    public void StreamBufferSource_RejectsNull()
        => Assert.Throws<ArgumentNullException>(() => new StreamBufferSource(null!));

    [Fact]
    public void StreamBufferSource_RejectsAWriteOnlyStream()
    {
        using var writeOnly = new WriteOnlyStream();

        Assert.Throws<ArgumentException>(() => new StreamBufferSource(writeOnly));
    }

    [Fact]
    public void StreamSource_ReadsThroughToTheOriginStream()
    {
        using var origin = new MemoryStream(Payload);
        var source = new StreamSource(origin);

        var stream = source.GetStream();
        stream.Position = 1;

        Assert.Equal(Payload.Length, stream.Length);
        Assert.Equal(1, origin.Position);
    }

    [Fact]
    public void StreamSource_DoesNotCloseTheStreamItDoesNotOwn()
    {
        // The library disposes the streams it hands out, but this one belongs to the caller.
        using var origin = new MemoryStream(Payload);
        var source = new StreamSource(origin);

        using (var stream = source.GetStream())
            stream.ReadByte();

        Assert.True(origin.CanRead);

        var second = source.GetStream();
        second.Position = 0;
        Assert.Equal(Payload, Drain(second));
    }

    [Fact]
    public void StreamSource_ReportsTheClosedStreamAsDisposedRatherThanAsABadArgument()
    {
        // Nothing is passed to GetStream, so an ArgumentException would be misleading — and naming
        // a private field as its parameter would leak an implementation detail to the caller.
        var origin = new MemoryStream(Payload);
        var source = new StreamSource(origin);
        origin.Dispose();

        var exception = Assert.Throws<ObjectDisposedException>(() => source.GetStream());

        Assert.Equal(nameof(MemoryStream), exception.ObjectName);
        Assert.DoesNotContain("_sourceStream", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void StreamSource_RejectsNull() => Assert.Throws<ArgumentNullException>(() => new StreamSource(null!));

    [Fact]
    public void StreamSource_RejectsAnUnseekableStream()
    {
        using var unseekable = new UnseekableStream(Payload);

        Assert.Throws<ArgumentException>(() => new StreamSource(unseekable));
    }

    private sealed class WriteOnlyStream : MemoryStream
    {
        public override bool CanRead => false;
    }

    private sealed class UnseekableStream(byte[] data) : MemoryStream(data)
    {
        public override bool CanSeek => false;

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
    }
}
