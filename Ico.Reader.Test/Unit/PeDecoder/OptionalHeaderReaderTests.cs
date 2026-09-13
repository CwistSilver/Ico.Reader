using Ico.Reader.PeDecoder.Models;
using Ico.Reader.PeDecoder.Reading;

namespace Ico.Reader.Test.Unit.PeDecoder;

/// <summary>
/// PE32+ drops BaseOfData and widens ImageBase and the four stack and heap sizes to 64 bits, which moves the fields
/// after them. Every value here is distinct and the 64-bit ones do not fit in 32 bits, so a field read from the wrong
/// offset or at the wrong width shows up as a wrong number.
/// </summary>
public sealed class OptionalHeaderReaderTests
{
    private const int Pe32Size = 224;
    private const int Pe32PlusSize = 240;

    [Fact]
    public void Read_Pe32_ReadsEveryField()
    {
        var bytes = new HeaderWriter(Pe32Size)
            .UInt16(0, (ushort)MagicNumber.PE32)
            .Byte(2, 14)
            .Byte(3, 38)
            .UInt32(4, 0x1004)
            .UInt32(8, 0x2008)
            .UInt32(12, 0x300C)
            .UInt32(16, 0x4010)
            .UInt32(20, 0x5014)
            .UInt32(24, 0x6018)
            .UInt32(28, 0x1000_001C)
            .UInt32(32, 0x7020)
            .UInt32(36, 0x8024)
            .UInt16(40, 0x0128)
            .UInt16(42, 0x012A)
            .UInt16(44, 0x012C)
            .UInt16(46, 0x012E)
            .UInt16(48, 0x0130)
            .UInt16(50, 0x0132)
            .UInt32(52, 0x9034)
            .UInt32(56, 0xA038)
            .UInt32(60, 0xB03C)
            .UInt32(64, 0xC040)
            .UInt16(68, 0x0144)
            .UInt16(70, 0x0146)
            .UInt32(72, 0xD048)
            .UInt32(76, 0xE04C)
            .UInt32(80, 0xF050)
            .UInt32(84, 0x1_0054)
            .UInt32(88, 0x1_1058)
            .UInt32(92, 16)
            .Build();

        var header = OptionalHeaderReader.Read(new MemoryStream(bytes), 0, Pe32Size);

        Assert.Equivalent(new
        {
            Magic = MagicNumber.PE32,
            MajorLinkerVersion = (byte)14,
            MinorLinkerVersion = (byte)38,
            SizeOfCode = 0x1004u,
            SizeOfInitializedData = 0x2008u,
            SizeOfUninitializedData = 0x300Cu,
            AddressOfEntryPoint = 0x4010u,
            BaseOfCode = 0x5014u,
            BaseOfData = 0x6018u,
            ImageBase = 0x1000_001CUL,
            SectionAlignment = 0x7020u,
            FileAlignment = 0x8024u,
            MajorOperatingSystemVersion = (ushort)0x0128,
            MinorOperatingSystemVersion = (ushort)0x012A,
            MajorImageVersion = (ushort)0x012C,
            MinorImageVersion = (ushort)0x012E,
            MajorSubsystemVersion = (ushort)0x0130,
            MinorSubsystemVersion = (ushort)0x0132,
            Win32VersionValue = 0x9034u,
            SizeOfImage = 0xA038u,
            SizeOfHeaders = 0xB03Cu,
            CheckSum = 0xC040u,
            Subsystem = (ushort)0x0144,
            DllCharacteristics = (ushort)0x0146,
            SizeOfStackReserve = 0xD048UL,
            SizeOfStackCommit = 0xE04CUL,
            SizeOfHeapReserve = 0xF050UL,
            SizeOfHeapCommit = 0x1_0054UL,
            LoaderFlags = 0x1_1058u,
            NumberOfRvaAndSizes = 16u,
        }, header);
    }

    [Fact]
    public void Read_Pe32Plus_ReadsEveryFieldAtItsOwnOffsetAndWidth()
    {
        var bytes = new HeaderWriter(Pe32PlusSize)
            .UInt16(0, (ushort)MagicNumber.PE32Plus)
            .Byte(2, 14)
            .Byte(3, 38)
            .UInt32(4, 0x1004)
            .UInt32(8, 0x2008)
            .UInt32(12, 0x300C)
            .UInt32(16, 0x4010)
            .UInt32(20, 0x5014)
            .UInt64(24, 0x1_4000_0018)
            .UInt32(32, 0x7020)
            .UInt32(36, 0x8024)
            .UInt16(40, 0x0128)
            .UInt16(42, 0x012A)
            .UInt16(44, 0x012C)
            .UInt16(46, 0x012E)
            .UInt16(48, 0x0130)
            .UInt16(50, 0x0132)
            .UInt32(52, 0x9034)
            .UInt32(56, 0xA038)
            .UInt32(60, 0xB03C)
            .UInt32(64, 0xC040)
            .UInt16(68, 0x0144)
            .UInt16(70, 0x0146)
            .UInt64(72, 0x2_0000_0048)
            .UInt64(80, 0x3_0000_0050)
            .UInt64(88, 0x4_0000_0058)
            .UInt64(96, 0x5_0000_0060)
            .UInt32(104, 0x1_1068)
            .UInt32(108, 16)
            .Build();

        var header = OptionalHeaderReader.Read(new MemoryStream(bytes), 0, Pe32PlusSize);

        Assert.Equivalent(new
        {
            Magic = MagicNumber.PE32Plus,
            MajorLinkerVersion = (byte)14,
            MinorLinkerVersion = (byte)38,
            SizeOfCode = 0x1004u,
            SizeOfInitializedData = 0x2008u,
            SizeOfUninitializedData = 0x300Cu,
            AddressOfEntryPoint = 0x4010u,
            BaseOfCode = 0x5014u,
            BaseOfData = 0u,
            ImageBase = 0x1_4000_0018UL,
            SectionAlignment = 0x7020u,
            FileAlignment = 0x8024u,
            MajorOperatingSystemVersion = (ushort)0x0128,
            MinorOperatingSystemVersion = (ushort)0x012A,
            MajorImageVersion = (ushort)0x012C,
            MinorImageVersion = (ushort)0x012E,
            MajorSubsystemVersion = (ushort)0x0130,
            MinorSubsystemVersion = (ushort)0x0132,
            Win32VersionValue = 0x9034u,
            SizeOfImage = 0xA038u,
            SizeOfHeaders = 0xB03Cu,
            CheckSum = 0xC040u,
            Subsystem = (ushort)0x0144,
            DllCharacteristics = (ushort)0x0146,
            SizeOfStackReserve = 0x2_0000_0048UL,
            SizeOfStackCommit = 0x3_0000_0050UL,
            SizeOfHeapReserve = 0x4_0000_0058UL,
            SizeOfHeapCommit = 0x5_0000_0060UL,
            LoaderFlags = 0x1_1068u,
            NumberOfRvaAndSizes = 16u,
        }, header);
    }

    /// <summary>
    /// Lays out an optional header behind the space the PE signature and COFF header take up, where the reader looks
    /// for it.
    /// </summary>
    private sealed class HeaderWriter(int size)
    {
        private readonly byte[] _bytes = new byte[PeHeader.PeHeaderSize + size];

        public HeaderWriter Byte(int offset, byte value)
        {
            _bytes[PeHeader.PeHeaderSize + offset] = value;
            return this;
        }

        public HeaderWriter UInt16(int offset, ushort value) => Write(offset, BitConverter.GetBytes(value));

        public HeaderWriter UInt32(int offset, uint value) => Write(offset, BitConverter.GetBytes(value));

        public HeaderWriter UInt64(int offset, ulong value) => Write(offset, BitConverter.GetBytes(value));

        public byte[] Build() => _bytes;

        private HeaderWriter Write(int offset, byte[] value)
        {
            value.CopyTo(_bytes, (int)PeHeader.PeHeaderSize + offset);
            return this;
        }
    }
}
