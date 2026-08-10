namespace Ico.Reader.Test.Unit.Data;

public sealed class IcoHeaderTests
{
    [Fact]
    public void ReadFromStream_ReadsTheThreeHeaderFields()
    {
        var data = IcoBuilder.Icon()
            .AddIcon(16, 16, 32, new byte[8])
            .AddIcon(32, 32, 32, new byte[8])
            .Build();

        var header = IcoHeader.ReadFromStream(new MemoryStream(data));

        Assert.Equal(0, header.Reserved);
        Assert.Equal(1, header.ImageType);
        Assert.Equal(2, header.ImageCount);
    }

    [Fact]
    public void ReadFromStream_ReadsCursorHeaders()
    {
        var data = IcoBuilder.Cursor().AddCursor(16, 16, 4, 7, new byte[8]).Build();

        var header = IcoHeader.ReadFromStream(new MemoryStream(data));

        Assert.Equal(2, header.ImageType);
        Assert.Equal(1, header.ImageCount);
    }

    [Fact]
    public void ReadFromStream_HonoursTheStartPosition()
    {
        var ico = IcoBuilder.Icon().AddIcon(16, 16, 32, new byte[8]).Build();
        var padded = new byte[10 + ico.Length];
        ico.CopyTo(padded, 10);

        var header = IcoHeader.ReadFromStream(new MemoryStream(padded), 10);

        Assert.Equal(1, header.ImageType);
        Assert.Equal(1, header.ImageCount);
    }
}
