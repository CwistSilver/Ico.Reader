using Ico.Reader.Data;
using Ico.Reader.Data.Source;
using Ico.Reader.Reading;

namespace Ico.Reader;

/// <summary>
/// Provides functionality to read ico data from files, byte arrays, or streams, and convert them into IcoData objects for further processing or display.
/// </summary>
public sealed class IcoReader
{
    private const string DefaultGroupName = "1";

    private readonly IcoReaderConfiguration _icoReaderConfiguration;

    /// <summary>
    /// Initializes a new instance of the icoReader class with a specific configuration.
    /// </summary>
    /// <param name="icoReaderConfiguration">The configuration settings to use for reading ico's.</param>
    public IcoReader(IcoReaderConfiguration icoReaderConfiguration)
    {
        _icoReaderConfiguration = icoReaderConfiguration;
    }

    /// <summary>
    /// Initializes a new instance of the icoReader class with default configuration settings.
    /// </summary>
    public IcoReader()
    {
        _icoReaderConfiguration = new IcoReaderConfiguration();
    }

    /// <summary>
    /// Reads ico data from a specified file path.
    /// </summary>
    /// <param name="filePath">The path to the file containing the ico data.</param>
    /// <returns>An IcoData object containing the read ico data, or null if the file does not exist or cannot be read.</returns>
    public IcoData? Read(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        var icoSource = new PathSource(filePath);
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        var icoData = ReadFromStream(stream, icoSource);
        if (icoData is null)
            return null;

        icoData.Name = Path.GetFileNameWithoutExtension(filePath);
        return icoData;
    }

    /// <summary>
    /// Reads ico data from a specified file path, reading the file asynchronously.
    /// </summary>
    /// <remarks>
    /// Parsing itself is a series of small seeks and so runs synchronously once the file is in
    /// memory. That buffer is released as soon as parsing finishes; individual images are still read
    /// lazily from the file, exactly as with <see cref="Read(string)"/>.
    /// </remarks>
    /// <param name="filePath">The path to the file containing the ico data.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>An IcoData object containing the read ico data, or null if the file does not exist or cannot be read.</returns>
    public async Task<IcoData?> ReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            return null;

        var icoSource = new PathSource(filePath);
        var buffer = await ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);

        using var stream = new MemoryStream(buffer, writable: false);

        var icoData = ReadFromStream(stream, icoSource);
        if (icoData is null)
            return null;

        icoData.Name = Path.GetFileNameWithoutExtension(filePath);
        return icoData;
    }

    /// <summary>
    /// Reads ico data from a stream, copying it asynchronously.
    /// </summary>
    /// <param name="stream">The stream containing the ico data.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>An IcoData object containing the read ico data, or null if the data cannot be read.</returns>
    public async Task<IcoData?> ReadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, 81920, cancellationToken).ConfigureAwait(false);

        return Read(buffer.ToArray());
    }

    private static async Task<byte[]> ReadAllBytesAsync(string filePath, CancellationToken cancellationToken)
    {
        using var file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        using var buffer = new MemoryStream();

        await file.CopyToAsync(buffer, 81920, cancellationToken).ConfigureAwait(false);

        return buffer.ToArray();
    }

    /// <summary>
    /// Reads ico data from a byte array.
    /// </summary>
    /// <param name="data">The byte array containing the ico data.</param>
    /// <returns>An IcoData object containing the read ico data, or null if the data cannot be read.</returns>
    public IcoData? Read(byte[] data)
    {
        MemoryStream stream = new(data, false);
        var icoSource = new MemorySource(data);
        return ReadFromStream(stream, icoSource);
    }

    /// <summary>
    /// Reads ico data from a stream.
    /// </summary>
    /// <param name="stream">The stream containing the ico data.</param>
    /// <param name="copyStream">If true, a copy of the stream will be created; otherwise, the original stream will be used.</param>
    /// <returns>An IcoData object containing the read ico data, or null if the data cannot be read.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the provided stream is null.</exception>
    public IcoData? Read(Stream stream, bool copyStream = true)
    {
        IDataSource dataSource;
        if (copyStream)
            dataSource = new StreamBufferSource(stream);
        else
            dataSource = new StreamSource(stream);

        return ReadFromStream(stream, dataSource);
    }

    /// <summary>
    /// Reads ico data from a stream using a specified ico source.
    /// </summary>
    /// <param name="stream">The stream containing the ico data.</param>
    /// <param name="dataSource">The ico source providing the stream.</param>
    /// <returns>An IcoData object containing the read ico data, or null if the data cannot be read.</returns>
    private IcoData? ReadFromStream(Stream stream, IDataSource dataSource)
    {
        try
        {
            return _icoReaderConfiguration.IcoExeDecoder.IsPeFormat(stream)
                ? ReadFromExe(stream, dataSource)
                : ReadFromIco(stream, dataSource);
        }
        catch (Exception exception) when (exception is EndOfStreamException or InvalidDataException)
        {
            // Truncated or malformed input is a parse failure, and every Read overload reports those as null.
            return null;
        }
    }

    /// <summary>
    /// Reads ico data from an executable file stream.
    /// </summary>
    /// <param name="stream">The stream representing the executable file.</param>
    /// <param name="dataSource">The ico source providing the stream.</param>
    /// <returns>An IcoData object if ico data is successfully read, otherwise null.</returns>
    private IcoData? ReadFromExe(Stream stream, IDataSource dataSource)
    {
        if (!_icoReaderConfiguration.IcoExeDecoder.IsPeFormat(stream))
            return null;

        var decodedicoResult = _icoReaderConfiguration.IcoExeDecoder.GetDecodedIcoResult(stream, _icoReaderConfiguration.IcoDecoder);
        if (decodedicoResult is null)
            return null;

        return new IcoData(_icoReaderConfiguration.IcoDecoder, dataSource, decodedicoResult);
    }

    private IcoData? ReadFromIco(Stream stream, IDataSource dataSource)
    {
        // The stream may belong to the caller, so an unreadable file is reported by returning null
        // and never by closing it.
        var header = IcoHeaderReader.Read(stream);
        if (header.Reserved != 0)
            return null;

        var originFileType = header.ImageType switch
        {
            IconDirectoryEntry.ImageType => IcoOriginFileType.Ico,
            CursorDirectoryEntry.ImageType => IcoOriginFileType.Cur,
            _ => (IcoOriginFileType?)null
        };

        if (originFileType is null)
            return null;

        var directoryEntries = DirectoryEntryParser.ReadFileEntries(stream, header);
        var references = new List<ImageReference>(directoryEntries.Length);

        for (var i = 0; i < directoryEntries.Length; i++)
        {
            if (directoryEntries[i] is IconDirectoryEntry { Reserved: not 0 })
                return null;

            var imageReference = ImageReferenceReader.FromDirectoryEntry(stream, directoryEntries[i], _icoReaderConfiguration.IcoDecoder);
            if (imageReference is null)
                return null;

            references.Add(imageReference with { Id = i });
        }

        var decodedIcoResult = new DecodedIcoResult
        {
            OriginFileType = originFileType.Value,
            References = references,
            IcoGroups = [CreateGroup(header, directoryEntries)]
        };

        return new IcoData(_icoReaderConfiguration.IcoDecoder, dataSource, decodedIcoResult);
    }

    /// <summary>
    /// A standalone ICO or CUR file has no grouping of its own, so every image is placed in a single
    /// group named "1" to match how groups are exposed for EXE and DLL sources.
    /// </summary>
    private static IIcoGroup CreateGroup(IcoHeader header, IIcoDirectoryEntry[] directoryEntries)
        => header.ImageType == IconDirectoryEntry.ImageType
            ? new IconGroup { Name = DefaultGroupName, Header = header, DirectoryEntries = [.. directoryEntries.Cast<IconDirectoryEntry>()] }
            : new CursorGroup { Name = DefaultGroupName, Header = header, DirectoryEntries = [.. directoryEntries.Cast<CursorDirectoryEntry>()] };
}
