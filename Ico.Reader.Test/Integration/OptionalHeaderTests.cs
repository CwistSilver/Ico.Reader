using System.Reflection.PortableExecutable;

using Ico.Reader.PeDecoder;
using Ico.Reader.PeDecoder.Models;

namespace Ico.Reader.Test.Integration;

/// <summary>
/// Reads the optional header of real images the way System.Reflection.Metadata does: the PE fixture, which is PE32, and
/// the runtime's core library, which a 64-bit test process loads as PE32+.
/// </summary>
public sealed class OptionalHeaderTests
{
    public static TheoryData<string> Images => new() { TestFiles.PeFixture, typeof(object).Assembly.Location };

    [Theory]
    [MemberData(nameof(Images))]
    public void DecodePE_ReadsTheOptionalHeaderLikeSystemReflectionMetadata(string path)
    {
        using var stream = File.OpenRead(path);
        var actual = new PeFileDecoder().DecodePE(stream).Optional!;

        stream.Position = 0;
        using var reader = new PEReader(stream, PEStreamOptions.LeaveOpen);
        var expected = reader.PEHeaders.PEHeader!;

        Assert.Equivalent(FieldsOf(expected), FieldsOf(actual));
    }

    [Fact]
    public void Images_CoverBothFormats()
    {
        Assert.SkipUnless(Environment.Is64BitProcess, "The runtime's core library is PE32+ only in a 64-bit process.");

        Assert.Equal(
            [MagicNumber.PE32, MagicNumber.PE32Plus],
            Images.Select(row => MagicOf(row.Data)).OrderBy(magic => magic));
    }

    private static MagicNumber MagicOf(string path)
    {
        using var stream = File.OpenRead(path);
        return new PeFileDecoder().DecodePE(stream).Optional!.Magic;
    }

    private static object FieldsOf(PEHeader header) => new
    {
        Magic = (ushort)header.Magic,
        header.MajorLinkerVersion,
        header.MinorLinkerVersion,
        SizeOfCode = (uint)header.SizeOfCode,
        SizeOfInitializedData = (uint)header.SizeOfInitializedData,
        SizeOfUninitializedData = (uint)header.SizeOfUninitializedData,
        AddressOfEntryPoint = (uint)header.AddressOfEntryPoint,
        BaseOfCode = (uint)header.BaseOfCode,
        BaseOfData = (uint)header.BaseOfData,
        header.ImageBase,
        SectionAlignment = (uint)header.SectionAlignment,
        FileAlignment = (uint)header.FileAlignment,
        header.MajorOperatingSystemVersion,
        header.MinorOperatingSystemVersion,
        header.MajorImageVersion,
        header.MinorImageVersion,
        header.MajorSubsystemVersion,
        header.MinorSubsystemVersion,
        SizeOfImage = (uint)header.SizeOfImage,
        SizeOfHeaders = (uint)header.SizeOfHeaders,
        header.CheckSum,
        Subsystem = (ushort)header.Subsystem,
        DllCharacteristics = (ushort)header.DllCharacteristics,
        header.SizeOfStackReserve,
        header.SizeOfStackCommit,
        header.SizeOfHeapReserve,
        header.SizeOfHeapCommit,
        NumberOfRvaAndSizes = (uint)header.NumberOfRvaAndSizes,
        ResourceTableAddress = (uint)header.ResourceTableDirectory.RelativeVirtualAddress,
        ResourceTableSize = (uint)header.ResourceTableDirectory.Size,
    };

    private static object FieldsOf(OptionalHeader header) => new
    {
        Magic = (ushort)header.Magic,
        header.MajorLinkerVersion,
        header.MinorLinkerVersion,
        header.SizeOfCode,
        header.SizeOfInitializedData,
        header.SizeOfUninitializedData,
        header.AddressOfEntryPoint,
        header.BaseOfCode,
        header.BaseOfData,
        ImageBase = (ulong)header.ImageBase,
        header.SectionAlignment,
        header.FileAlignment,
        header.MajorOperatingSystemVersion,
        header.MinorOperatingSystemVersion,
        header.MajorImageVersion,
        header.MinorImageVersion,
        header.MajorSubsystemVersion,
        header.MinorSubsystemVersion,
        header.SizeOfImage,
        header.SizeOfHeaders,
        header.CheckSum,
        header.Subsystem,
        header.DllCharacteristics,
        SizeOfStackReserve = (ulong)header.SizeOfStackReserve,
        SizeOfStackCommit = (ulong)header.SizeOfStackCommit,
        SizeOfHeapReserve = (ulong)header.SizeOfHeapReserve,
        SizeOfHeapCommit = (ulong)header.SizeOfHeapCommit,
        header.NumberOfRvaAndSizes,
        ResourceTableAddress = header.ResourceTable!.VirtualAddress,
        ResourceTableSize = header.ResourceTable.Size,
    };
}
