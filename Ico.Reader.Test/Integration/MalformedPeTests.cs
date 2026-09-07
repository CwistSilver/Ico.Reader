using Ico.Reader.PeDecoder;
using Ico.Reader.PeDecoder.Models;
using Ico.Reader.PeDecoder.Reading;

namespace Ico.Reader.Test.Integration;

/// <summary>
/// Every length and offset in a PE file is attacker controlled. These cases feed the decoder headers
/// that declare absurd sizes and confirm it fails as an ordinary exception instead of exhausting the
/// stack, which would be an uncatchable process kill.
/// </summary>
public sealed class MalformedPeTests
{
    private readonly IcoReader _reader = new();

    private static byte[] PeFixtureBytes() => File.ReadAllBytes(TestFiles.PeFixture);

    private static int PeHeaderOffset(byte[] pe) => BitConverter.ToInt32(pe, 60);

    [Fact]
    public void Read_SurvivesAnAbsurdSectionCount()
    {
        // NumberOfSections sits two bytes into the COFF header and drives a
        // NumberOfSections * 40 byte buffer, which at ushort.MaxValue is about 2.6 MB.
        var pe = PeFixtureBytes();
        BitConverter.GetBytes(ushort.MaxValue).CopyTo(pe, PeHeaderOffset(pe) + 6);

        var exception = Record.Exception(() => _reader.Read(pe));

        Assert.True(exception is null or IOException or InvalidDataException or ArgumentException or IndexOutOfRangeException or ArgumentOutOfRangeException,
            $"Unexpected exception type: {exception}");
    }

    [Fact]
    public void Read_SurvivesAnAbsurdOptionalHeaderSize()
    {
        var pe = PeFixtureBytes();
        BitConverter.GetBytes(ushort.MaxValue).CopyTo(pe, PeHeaderOffset(pe) + 20);

        var exception = Record.Exception(() => _reader.Read(pe));

        Assert.True(exception is null or IOException or InvalidDataException or ArgumentException or IndexOutOfRangeException or ArgumentOutOfRangeException,
            $"Unexpected exception type: {exception}");
    }

    [Fact]
    public void Read_SurvivesATruncatedPe()
    {
        var pe = PeFixtureBytes();
        var truncated = pe.AsSpan(0, pe.Length / 4).ToArray();

        var exception = Record.Exception(() => _reader.Read(truncated));

        Assert.True(exception is null or IOException or InvalidDataException or ArgumentException or IndexOutOfRangeException or ArgumentOutOfRangeException,
            $"Unexpected exception type: {exception}");
    }

    [Fact]
    public void Read_SurvivesACyclicResourceTree()
    {
        // Subdirectory offsets are relative to the start of the resource section. Root entry 0 points
        // at a resource-type directory; making that directory's own first entry point back at itself
        // turns the tree into a loop, which without a cycle guard recurses until the stack gives out.
        var pe = PeFixtureBytes();
        var sectionStart = ResourceSectionStart(pe);

        var typeDirectoryOffset = BitConverter.ToUInt32(pe, sectionStart + DirectoryHeaderSize + SubdirectoryOffsetField) & 0x7FFFFFFF;
        Assert.NotEqual(0u, typeDirectoryOffset);

        var selfReference = sectionStart + (int)typeDirectoryOffset + DirectoryHeaderSize + SubdirectoryOffsetField;
        BitConverter.GetBytes(0x80000000u | typeDirectoryOffset).CopyTo(pe, selfReference);

        var exception = Record.Exception(() => _reader.Read(pe));

        Assert.True(exception is null or IOException or InvalidDataException or ArgumentException or IndexOutOfRangeException or ArgumentOutOfRangeException,
            $"Unexpected exception type: {exception}");
    }

    private const int DirectoryHeaderSize = 16;
    private const int SubdirectoryOffsetField = 4;

    private static int ResourceSectionStart(byte[] pe)
    {
        using var stream = new MemoryStream(pe);
        var peHeader = new PeFileDecoder().DecodePE(stream);
        var sections = SectionHeaderReader.Read(stream, peHeader);
        var resourceTable = peHeader.Optional!.ResourceTable!;

        return (int)resourceTable.FindFileSectionHeader(sections).PointerToRawData;
    }

    [Fact]
    public void Read_StillReadsTheUntouchedFixture()
    {
        // Positive control: the corruptions above are the only reason the cases fail.
        var pe = _reader.Read(PeFixtureBytes());

        Assert.NotNull(pe);
        Assert.Equal(13, pe.ImageReferences.Count);
    }
}
