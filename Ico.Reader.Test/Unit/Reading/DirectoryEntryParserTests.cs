using Ico.Reader.Reading;

namespace Ico.Reader.Test.Unit.Reading;

public sealed class DirectoryEntryParserTests
{
    private static Stream EntriesStream(byte[] ico)
    {
        var stream = new MemoryStream(ico) { Position = 6 };
        return stream;
    }

    [Fact]
    public void ReadEntriesFromStream_ReadsIconFields()
    {
        var ico = IcoBuilder.Icon()
            .AddIcon(48, 32, 8, new byte[10], colorCount: 16)
            .Build();

        var entries = DirectoryEntryParser.ReadFileEntries(EntriesStream(ico), IcoHeaderReader.Read(new MemoryStream(ico)));

        var entry = Assert.IsType<IconDirectoryEntry>(Assert.Single(entries));
        Assert.Equal(48, entry.Width);
        Assert.Equal(32, entry.Height);
        Assert.Equal(16, entry.ColorCount);
        Assert.Equal(0, entry.Reserved);
        Assert.Equal(1, entry.Planes);
        Assert.Equal(8, entry.ColorDepth);
        Assert.Equal(10u, entry.ImageSize);
        Assert.Equal(22u, entry.ImageOffset);
        Assert.Equal(entry.ImageOffset, entry.RealImageOffset);
    }

    [Fact]
    public void ReadEntriesFromStream_ReadsCursorHotspots()
    {
        var ico = IcoBuilder.Cursor()
            .AddCursor(32, 32, hotspotX: 9, hotspotY: 17, new byte[4])
            .Build();

        var entries = DirectoryEntryParser.ReadFileEntries(EntriesStream(ico), IcoHeaderReader.Read(new MemoryStream(ico)));

        var entry = Assert.IsType<CursorDirectoryEntry>(Assert.Single(entries));
        Assert.Equal(9, entry.HotspotX);
        Assert.Equal(17, entry.HotspotY);
        Assert.Equal(0, entry.Planes);
    }

    [Fact]
    public void ReadEntriesFromStream_ReadsEveryEntry()
    {
        var ico = IcoBuilder.Icon()
            .AddIcon(16, 16, 4, new byte[4])
            .AddIcon(32, 32, 8, new byte[8])
            .AddIcon(48, 48, 32, new byte[16])
            .Build();

        var entries = DirectoryEntryParser.ReadFileEntries(EntriesStream(ico), IcoHeaderReader.Read(new MemoryStream(ico)));

        Assert.Equal(3, entries.Length);
        Assert.Equal([16, 32, 48], entries.Select(x => (int)x.Width));
        Assert.Equal([4, 8, 32], entries.Select(x => (int)x.ColorDepth));
    }

    [Fact]
    public void ReadEntriesFromStream_ReturnsEmptyForAnEmptyDirectory()
    {
        var header = new IcoHeader { ImageType = 1, ImageCount = 0 };

        Assert.Empty(DirectoryEntryParser.ReadFileEntries(new MemoryStream(), header));
    }

    [Fact]
    public void ReadEntriesFromStream_ThrowsForAnUnknownImageType()
    {
        var header = new IcoHeader { ImageType = 3, ImageCount = 1 };

        Assert.Throws<NotSupportedException>(() => DirectoryEntryParser.ReadFileEntries(new MemoryStream(new byte[16]), header));
    }

    [Fact]
    public void ReadEntriesFromStream_DoesNotExhaustTheStackForALargeDirectory()
    {
        // ImageCount is a ushort taken straight from untrusted input; a stack-allocated buffer of
        // that size would take the process down rather than throw.
        var header = new IcoHeader { ImageType = 1, ImageCount = ushort.MaxValue };
        var stream = new MemoryStream(new byte[ushort.MaxValue * 16]);

        var entries = DirectoryEntryParser.ReadFileEntries(stream, header);

        Assert.Equal(ushort.MaxValue, entries.Length);
    }

    [Fact]
    public void ReadEntriesFromEXEStream_ReadsIconEntriesUsingTheFourteenByteLayout()
    {
        var stream = new MemoryStream(ExeEntry(width: 64, height: 64, colorDepth: 32, imageSize: 4096, resourceId: 7));
        var header = new IcoHeader { ImageType = 1, ImageCount = 1 };

        var entries = DirectoryEntryParser.ReadResourceEntries<IconDirectoryEntry>(stream, header);

        var entry = Assert.Single(entries);
        Assert.Equal(64, entry.Width);
        Assert.Equal(32, entry.ColorDepth);
        Assert.Equal(4096u, entry.ImageSize);
        Assert.Equal(7u, entry.ImageOffset);
    }

    [Fact]
    public void ReadEntriesFromEXEStream_ReadsCursorEntries()
    {
        var stream = new MemoryStream(ExeEntry(width: 32, height: 64, colorDepth: 0, imageSize: 256, resourceId: 3));
        var header = new IcoHeader { ImageType = 2, ImageCount = 1 };

        var entries = DirectoryEntryParser.ReadResourceEntries<CursorDirectoryEntry>(stream, header);

        var entry = Assert.Single(entries);
        Assert.Equal(32, entry.Width);
        Assert.Equal(256u, entry.ImageSize);
        Assert.Equal(3u, entry.ImageOffset);
    }

    [Fact]
    public void ReadEntriesFromEXEStream_RejectsAMismatchedEntryType()
    {
        var stream = new MemoryStream(new byte[14]);
        var header = new IcoHeader { ImageType = 1, ImageCount = 1 };

        Assert.Throws<ArgumentException>(() => DirectoryEntryParser.ReadResourceEntries<CursorDirectoryEntry>(stream, header));
    }

    [Fact]
    public void ReadEntriesFromEXEStream_DoesNotExhaustTheStackForALargeDirectory()
    {
        var header = new IcoHeader { ImageType = 1, ImageCount = ushort.MaxValue };
        var stream = new MemoryStream(new byte[ushort.MaxValue * 14]);

        Assert.Equal(ushort.MaxValue, DirectoryEntryParser.ReadResourceEntries<IconDirectoryEntry>(stream, header).Length);
    }

    [Fact]
    public void ReadEntriesFromEXEStream_RejectsIconEntriesForACursorHeader()
    {
        var header = new IcoHeader { ImageType = 2, ImageCount = 1 };

        Assert.Throws<ArgumentException>(() => DirectoryEntryParser.ReadResourceEntries<IconDirectoryEntry>(new MemoryStream(new byte[14]), header));
    }

    private static byte[] ExeEntry(byte width, byte height, ushort colorDepth, uint imageSize, ushort resourceId)
    {
        var entry = new byte[14];
        entry[0] = width;
        entry[1] = height;
        BitConverter.GetBytes((ushort)1).CopyTo(entry, 4);
        BitConverter.GetBytes(colorDepth).CopyTo(entry, 6);
        BitConverter.GetBytes(imageSize).CopyTo(entry, 8);
        BitConverter.GetBytes(resourceId).CopyTo(entry, 12);

        return entry;
    }
}
