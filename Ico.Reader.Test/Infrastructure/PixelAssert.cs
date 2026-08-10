namespace Ico.Reader.Test.Infrastructure;

internal static class PixelAssert
{
    /// <summary>
    /// Asserts that decoded pixels match the reference pixels exactly, including the colour stored
    /// underneath fully transparent pixels.
    /// </summary>
    public static void Matches(byte[] expected, byte[] actual, int width, int height)
    {
        Assert.Equal(width * height * 4, actual.Length);
        Assert.Equal(expected.Length, actual.Length);

        for (var i = 0; i < expected.Length; i += 4)
        {
            var pixel = i / 4;
            var x = pixel % width;
            var y = pixel / width;

            AssertChannel(expected[i], actual[i], x, y, "red");
            AssertChannel(expected[i + 1], actual[i + 1], x, y, "green");
            AssertChannel(expected[i + 2], actual[i + 2], x, y, "blue");
            AssertChannel(expected[i + 3], actual[i + 3], x, y, "alpha");
        }
    }

    private static void AssertChannel(byte expected, byte actual, int x, int y, string channel)
    {
        if (expected != actual)
            Assert.Fail($"Pixel ({x},{y}) {channel}: expected {expected}, got {actual}.");
    }
}
