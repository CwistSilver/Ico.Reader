using System.Text;

namespace Ico.Reader.Test.Unit.Shims;

/// <summary>
/// Ico.Reader targets netstandard2.0 and compiles these shims in place of BCL members that only
/// exist on newer targets, so every read it performs goes through them.
/// <para>
/// The test project itself targets net8.0, where the BCL instance methods would win over the
/// extensions, so each shim is invoked through its declaring type rather than as an extension
/// method. These cases cover the edges the fixtures do not reach; the rest of the suite exercises
/// the shims end to end.
/// </para>
/// </summary>
public sealed class CommonShimsTests
{
    [Fact]
    public void StreamRead_FillsTheSpanAndReturnsTheCount()
    {
        using var stream = new MemoryStream([1, 2, 3, 4, 5]);
        Span<byte> buffer = new byte[5];

        var read = CommonShims.StreamExtensions.Read(stream, buffer);

        Assert.Equal(5, read);
        Assert.Equal<byte[]>([1, 2, 3, 4, 5], buffer.ToArray());
    }

    [Fact]
    public void StreamRead_StopsAtTheEndOfTheStream()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        Span<byte> buffer = new byte[8];

        var read = CommonShims.StreamExtensions.Read(stream, buffer);

        Assert.Equal(3, read);
        Assert.Equal<byte[]>([1, 2, 3], buffer.Slice(0, read).ToArray());
        Assert.Equal(0, buffer[3]);
    }

    [Fact]
    public void StreamRead_ContinuesFromTheCurrentPosition()
    {
        using var stream = new MemoryStream([1, 2, 3, 4, 5]) { Position = 2 };
        Span<byte> buffer = new byte[2];

        var read = CommonShims.StreamExtensions.Read(stream, buffer);

        Assert.Equal(2, read);
        Assert.Equal<byte[]>([3, 4], buffer.ToArray());
        Assert.Equal(4, stream.Position);
    }

    [Fact]
    public void StreamRead_HandlesAnEmptySpan()
    {
        using var stream = new MemoryStream([1, 2, 3]);

        var read = CommonShims.StreamExtensions.Read(stream, Span<byte>.Empty);

        Assert.Equal(0, read);
        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public void StreamRead_DoesNotLeakPooledBytesBeyondWhatItRead()
    {
        // The shim rents a shared buffer, so a previously used pool entry must not bleed into the
        // caller's span past the byte count it reports.
        var noisy = new byte[256];
        for (var i = 0; i < noisy.Length; i++)
            noisy[i] = 0xAB;

        using (var priming = new MemoryStream(noisy))
            _ = CommonShims.StreamExtensions.Read(priming, new byte[256]);

        using var stream = new MemoryStream([7, 7]);
        Span<byte> buffer = new byte[256];

        var read = CommonShims.StreamExtensions.Read(stream, buffer);

        Assert.Equal(2, read);
        Assert.All(buffer.Slice(read).ToArray(), b => Assert.Equal(0, b));
    }

    [Fact]
    public void StreamWrite_WritesTheWholeSpan()
    {
        using var stream = new MemoryStream();

        CommonShims.StreamExtensions.Write(stream, new byte[] { 9, 8, 7 }.AsSpan());

        Assert.Equal<byte[]>([9, 8, 7], stream.ToArray());
    }

    [Fact]
    public void StreamWrite_HandlesAnEmptySpan()
    {
        using var stream = new MemoryStream();

        CommonShims.StreamExtensions.Write(stream, ReadOnlySpan<byte>.Empty);

        Assert.Empty(stream.ToArray());
    }

    [Fact]
    public void BinaryWriterWrite_UsesTheBaseStreamForAPlainWriter()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        CommonShims.BinaryWriterExtensions.Write(writer, new byte[] { 1, 2, 3, 4 }.AsSpan());
        writer.Flush();

        Assert.Equal<byte[]>([1, 2, 3, 4], stream.ToArray());
    }

    [Fact]
    public void BinaryWriterWrite_FallsBackToTheArrayOverloadForADerivedWriter()
    {
        // The shim only bypasses the writer for the exact BinaryWriter type; a subclass may override
        // Write, so its own overload has to be used.
        using var stream = new MemoryStream();
        using var writer = new CountingBinaryWriter(stream);

        CommonShims.BinaryWriterExtensions.Write(writer, new byte[] { 5, 6, 7 }.AsSpan());
        writer.Flush();

        Assert.Equal(1, writer.ArrayWriteCount);
        Assert.Equal<byte[]>([5, 6, 7], stream.ToArray());
    }

    [Fact]
    public void SpanToStringFast_ReturnsEmptyForAnEmptySpan()
        => Assert.Same(string.Empty, CommonShims.SpanExtensions.ToStringFast(ReadOnlySpan<char>.Empty));

    [Fact]
    public void SpanToStringFast_CopiesOnlyTheSpannedCharacters()
    {
        var span = "abcdef".AsSpan(1, 3);

        Assert.Equal("bcd", CommonShims.SpanExtensions.ToStringFast(span));
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData(".text\0\0\0")]
    [InlineData("\0\0\0\0\0\0\0\0")]
    [InlineData(".rdata\0\0")]
    public void SpanToStringFast_MatchesTheStringConstructorItStandsInFor(string value)
    {
        // PeDecoder reads a section name through ToStringFast on netstandard2.0 and through
        // new string(span) everywhere else, so the two must agree — including on embedded nulls.
        Span<char> chars = value.ToCharArray();

        Assert.Equal(new string(chars), CommonShims.SpanExtensions.ToStringFast(chars));
        Assert.Equal(new string(chars).Trim('\0'), CommonShims.SpanExtensions.ToStringFast(chars).Trim('\0'));
    }

    [Fact]
    public void DictionaryTryAdd_AddsAMissingKey()
    {
        var dictionary = new Dictionary<string, int>();

        Assert.True(CommonShims.DictionaryExtensions.TryAdd(dictionary, "a", 1));
        Assert.Equal(1, dictionary["a"]);
    }

    [Fact]
    public void DictionaryTryAdd_LeavesAnExistingValueAlone()
    {
        var dictionary = new Dictionary<string, int> { ["a"] = 1 };

        Assert.False(CommonShims.DictionaryExtensions.TryAdd(dictionary, "a", 2));
        Assert.Equal(1, dictionary["a"]);
        Assert.Single(dictionary);
    }

    [Fact]
    public void EncodingGetString_ReturnsEmptyForNoBytes()
        => Assert.Equal(string.Empty, CommonShims.EncodingExtensions.GetString(Encoding.UTF8, ReadOnlySpan<byte>.Empty));

    [Fact]
    public void EncodingGetString_DecodesMultiByteCharacters()
    {
        var bytes = Encoding.UTF8.GetBytes("größe");

        Assert.Equal("größe", CommonShims.EncodingExtensions.GetString(Encoding.UTF8, bytes.AsSpan()));
    }

    [Fact]
    public void EncodingGetString_DecodesOnlyTheSpannedBytes()
    {
        var bytes = Encoding.ASCII.GetBytes("ABCDEF");

        Assert.Equal("CD", CommonShims.EncodingExtensions.GetString(Encoding.ASCII, bytes.AsSpan(2, 2)));
    }

    [Fact]
    public void EncodingGetChars_WritesTheDecodedCharactersAndReturnsTheCount()
    {
        var bytes = Encoding.ASCII.GetBytes("hello");
        Span<char> chars = new char[5];

        var written = CommonShims.EncodingExtensions.GetChars(Encoding.ASCII, bytes.AsSpan(), chars);

        Assert.Equal(5, written);
        Assert.Equal("hello", chars.Slice(0, written).ToString());
    }

    [Fact]
    public void EncodingGetChars_ReturnsTheCharacterCountRatherThanTheBufferLength()
    {
        // The destination is deliberately longer than the decoded text, so returning the buffer
        // length instead of the characters written would show up.
        var bytes = Encoding.ASCII.GetBytes("hi");
        Span<char> chars = new char[10];

        var written = CommonShims.EncodingExtensions.GetChars(Encoding.ASCII, bytes.AsSpan(), chars);

        Assert.Equal(2, written);
        Assert.Equal("hi", chars.Slice(0, written).ToString());
        Assert.Equal('\0', chars[2]);
    }

    [Fact]
    public void EncodingGetChars_CountsCharactersNotBytesForMultiByteText()
    {
        // "größe" takes more UTF-8 bytes than it has characters, so a buffer sized from the byte
        // count is longer than the decoded text.
        const string text = "größe";
        var bytes = Encoding.UTF8.GetBytes(text);
        Span<char> chars = new char[bytes.Length];

        var written = CommonShims.EncodingExtensions.GetChars(Encoding.UTF8, bytes.AsSpan(), chars);

        Assert.True(bytes.Length > text.Length, "Expected the sample text to be multi-byte in UTF-8.");
        Assert.Equal(text.Length, written);
        Assert.Equal(text, chars.Slice(0, written).ToString());
    }

    [Theory]
    [InlineData(0, 4)]
    [InlineData(4, 0)]
    public void EncodingGetChars_ReturnsZeroWhenEitherSideIsEmpty(int byteCount, int charCount)
    {
        var bytes = Encoding.ASCII.GetBytes(new string('x', byteCount));

        var written = CommonShims.EncodingExtensions.GetChars(Encoding.ASCII, bytes.AsSpan(), new char[charCount]);

        Assert.Equal(0, written);
    }

    private sealed class CountingBinaryWriter(Stream output) : BinaryWriter(output)
    {
        public int ArrayWriteCount { get; private set; }

        public override void Write(byte[] buffer, int index, int count)
        {
            ArrayWriteCount++;
            base.Write(buffer, index, count);
        }
    }
}
