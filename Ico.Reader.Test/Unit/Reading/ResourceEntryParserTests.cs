using Ico.Reader.Reading;

namespace Ico.Reader.Test.Unit.Reading;

public sealed class ResourceEntryParserTests
{
    private static byte[] ExeEntries(params (byte Width, byte Height, ushort ColorDepth, uint ImageSize, ushort ResourceId)[] entries)
    {
        var data = new byte[entries.Length * 14];
        for (var i = 0; i < entries.Length; i++)
        {
            var offset = i * 14;
            data[offset] = entries[i].Width;
            data[offset + 1] = entries[i].Height;
            BitConverter.GetBytes((ushort)1).CopyTo(data, offset + 4);
            BitConverter.GetBytes(entries[i].ColorDepth).CopyTo(data, offset + 6);
            BitConverter.GetBytes(entries[i].ImageSize).CopyTo(data, offset + 8);
            BitConverter.GetBytes(entries[i].ResourceId).CopyTo(data, offset + 12);
        }

        return data;
    }

    [Fact]
    public void ReadFromEXEStream_MapsTheResourceIdIntoImageOffset()
    {
        // Inside a PE the last field is a resource id, not a file offset; the decoder resolves it
        // later via RealImageOffset.
        var stream = new MemoryStream(ExeEntries((16, 16, 8, 100, 5), (32, 32, 32, 200, 9)));
        var header = new IcoHeader { ImageType = 1, ImageCount = 2 };

        var entries = DirectoryEntryParser.ReadResourceEntries(stream, header);

        Assert.Equal(2, entries.Length);
        Assert.Equal(5u, entries[0].ImageOffset);
        Assert.Equal(100u, entries[0].ImageSize);
        Assert.Equal(9u, entries[1].ImageOffset);
        Assert.Equal(32, entries[1].ColorDepth);
    }

    /// <summary>
    /// An RT_GROUP_CURSOR entry stores width and height as words, with the height doubled to cover
    /// the stacked AND mask — unlike an icon group, which uses two bytes.
    /// </summary>
    private static byte[] ExeCursorEntries(params (ushort Width, ushort Height, ushort ColorDepth, uint ImageSize, ushort ResourceId)[] entries)
    {
        var data = new byte[entries.Length * 14];
        for (var i = 0; i < entries.Length; i++)
        {
            var offset = i * 14;
            BitConverter.GetBytes(entries[i].Width).CopyTo(data, offset);
            BitConverter.GetBytes((ushort)(entries[i].Height * 2)).CopyTo(data, offset + 2);
            BitConverter.GetBytes((ushort)1).CopyTo(data, offset + 4);
            BitConverter.GetBytes(entries[i].ColorDepth).CopyTo(data, offset + 6);
            BitConverter.GetBytes(entries[i].ImageSize).CopyTo(data, offset + 8);
            BitConverter.GetBytes(entries[i].ResourceId).CopyTo(data, offset + 12);
        }

        return data;
    }

    [Fact]
    public void ReadFromEXEStream_ReadsCursorEntriesUsingTheWordLayout()
    {
        var stream = new MemoryStream(ExeCursorEntries((32, 32, 32, 512, 4)));
        var header = new IcoHeader { ImageType = 2, ImageCount = 1 };

        var entry = Assert.IsType<CursorDirectoryEntry>(Assert.Single(DirectoryEntryParser.ReadResourceEntries(stream, header)));

        Assert.Equal(32, entry.Width);
        Assert.Equal(32, entry.Height);
        Assert.Equal(1, entry.Planes);
        Assert.Equal(32, entry.ColorDepth);
        Assert.Equal(512u, entry.ImageSize);
        Assert.Equal(4u, entry.ImageOffset);
        Assert.Equal(0u, entry.RealImageOffset);
    }

    [Fact]
    public void ReadFromEXEStream_HalvesTheStackedCursorHeight()
    {
        var stream = new MemoryStream(ExeCursorEntries((16, 16, 4, 100, 1), (48, 48, 32, 200, 2)));
        var header = new IcoHeader { ImageType = 2, ImageCount = 2 };

        var entries = DirectoryEntryParser.ReadResourceEntries(stream, header);

        Assert.Equal([16, 48], entries.Select(x => (int)x.Width));
        Assert.Equal([16, 48], entries.Select(x => (int)x.Height));
    }

    [Fact]
    public void ReadFromEXEStream_KeepsThe256PixelConventionForCursors()
    {
        // A 256px cursor stores 256 and 512; both narrow to the 0 that means 256.
        var stream = new MemoryStream(ExeCursorEntries((256, 256, 32, 400, 1)));
        var header = new IcoHeader { ImageType = 2, ImageCount = 1 };

        var entry = Assert.Single(DirectoryEntryParser.ReadResourceEntries(stream, header));

        Assert.Equal(0, entry.Width);
        Assert.Equal(0, entry.Height);
    }

    [Fact]
    public void ReadFromEXEStream_ThrowsForAnUnknownImageType()
    {
        var header = new IcoHeader { ImageType = 9, ImageCount = 1 };

        Assert.Throws<NotSupportedException>(() => DirectoryEntryParser.ReadResourceEntries(new MemoryStream(new byte[14]), header));
    }

    [Fact]
    public void ReadFromEXEStream_DoesNotExhaustTheStackForALargeDirectory()
    {
        var header = new IcoHeader { ImageType = 1, ImageCount = ushort.MaxValue };
        var stream = new MemoryStream(new byte[ushort.MaxValue * 14]);

        Assert.Equal(ushort.MaxValue, DirectoryEntryParser.ReadResourceEntries(stream, header).Length);
    }

    [Fact]
    public void Size_ReflectsTheEntryCount()
    {
        var header = new IcoHeader { ImageType = 1, ImageCount = 2 };
        var group = new IconGroup
        {
            Name = "1",
            Header = header,
            DirectoryEntries = [new IconDirectoryEntry(), new IconDirectoryEntry()]
        };

        Assert.Equal(2, group.Size);
        Assert.Equal(IcoType.Icon, group.IcoType);

        var cursorGroup = new CursorGroup { Name = "1", Header = header, DirectoryEntries = [] };

        Assert.Equal(0, cursorGroup.Size);
        Assert.Equal(IcoType.Cursor, cursorGroup.IcoType);
    }
}
