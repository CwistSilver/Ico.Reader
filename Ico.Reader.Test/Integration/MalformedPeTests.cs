namespace Ico.Reader.Test.Integration;

/// <summary>
/// Every length and offset in a PE file is attacker controlled. These cases feed the decoder headers
/// that declare absurd sizes and confirm reading reports the file as unreadable, or reads what it can,
/// rather than throwing or exhausting the stack, which would be an uncatchable process kill.
/// </summary>
public sealed class MalformedPeTests
{
    private const int DirectoryHeaderSize = 16;
    private const int SubdirectoryOffsetField = 4;

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

        Assert.Null(Record.Exception(() => _reader.Read(pe)));
    }

    [Fact]
    public void Read_SurvivesAnAbsurdOptionalHeaderSize()
    {
        var pe = PeFixtureBytes();
        BitConverter.GetBytes(ushort.MaxValue).CopyTo(pe, PeHeaderOffset(pe) + 20);

        Assert.Null(Record.Exception(() => _reader.Read(pe)));
    }

    [Fact]
    public void Read_SurvivesATruncatedPe()
    {
        var pe = PeFixtureBytes();
        var truncated = pe.AsSpan(0, pe.Length / 4).ToArray();

        Assert.Null(Record.Exception(() => _reader.Read(truncated)));
    }

    [Fact]
    public void Read_SurvivesACyclicResourceTree()
    {
        // Subdirectory offsets count from the root of the resource tree. Root entry 0 points at a
        // resource-type directory; making that directory's own first entry point back at itself turns
        // the tree into a loop, which without a cycle guard recurses until the stack gives out.
        var pe = PeFixtureBytes();
        var root = PeResources.RootOffset(pe);

        var typeDirectoryOffset = BitConverter.ToUInt32(pe, root + DirectoryHeaderSize + SubdirectoryOffsetField) & 0x7FFFFFFF;
        Assert.NotEqual(0u, typeDirectoryOffset);

        var selfReference = root + (int)typeDirectoryOffset + DirectoryHeaderSize + SubdirectoryOffsetField;
        BitConverter.GetBytes(0x80000000u | typeDirectoryOffset).CopyTo(pe, selfReference);

        Assert.Null(Record.Exception(() => _reader.Read(pe)));
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
