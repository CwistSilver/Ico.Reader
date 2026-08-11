using Ico.Reader.Data;
using Ico.Reader.Data.Source;
using Ico.Reader.Reading;

namespace Ico.Reader;

/// <summary>
/// Provides functionality to read ico data from files, byte arrays, or streams, and convert them into IcoData objects for further processing or display.
/// </summary>
public sealed class IcoReader
{
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
        IcoData? IcoData;
        if (_icoReaderConfiguration.IcoExeDecoder.IsPeFormat(stream))
            IcoData = ReadFromExe(stream, dataSource);
        else
            IcoData = ReadFromIco(stream, dataSource);

        if (IcoData is null)
            return null;

        return IcoData;
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
        var decodedicoResult = GetDecodedIcoResult(header);
        if (decodedicoResult is null)
            return null;

        if (decodedicoResult.IcoGroups[0].Header!.Reserved != 0)
            return null;

        decodedicoResult.IcoGroups[0].DirectoryEntries = DirectoryEntryParser.ReadFileEntries(stream, decodedicoResult.IcoGroups[0].Header!);
        decodedicoResult.References = new List<ImageReference>(decodedicoResult.IcoGroups[0].DirectoryEntries!.Length);
        for (var i = 0; i < decodedicoResult.IcoGroups[0].DirectoryEntries!.Length; i++)
        {
            if (header.ImageType == IconDirectoryEntry.ImageType)
            {
                var icoHeader = (IconDirectoryEntry)decodedicoResult.IcoGroups[0].DirectoryEntries![i];
                if (icoHeader.Reserved != 0)
                    return null;
            }

            var imageReference = ImageReferenceReader.FromDirectoryEntry(stream, decodedicoResult.IcoGroups[0].DirectoryEntries![i], _icoReaderConfiguration.IcoDecoder);
            if (imageReference is null)
                return null;

            decodedicoResult.References.Add(imageReference.WithId(i));
        }

        return new IcoData(_icoReaderConfiguration.IcoDecoder, dataSource, decodedicoResult);
    }

    private DecodedIcoResult? GetDecodedIcoResult(IcoHeader header)
    {
        DecodedIcoResult decodedicoResult;
        if (header.ImageType == IconDirectoryEntry.ImageType)
        {
            decodedicoResult = new DecodedIcoResult { OriginFileType = IcoOriginFileType.Ico };
            decodedicoResult.IcoGroups.Add(new IconGroup() { Name = "1", Header = header });
        }
        else if (header.ImageType == CursorDirectoryEntry.ImageType)
        {
            decodedicoResult = new DecodedIcoResult { OriginFileType = IcoOriginFileType.Cur };
            decodedicoResult.IcoGroups.Add(new CursorGroup() { Name = "1", Header = header });
        }
        else
        {
            return null;
        }

        return decodedicoResult;
    }
}
