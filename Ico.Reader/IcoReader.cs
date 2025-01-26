using Ico.Reader.Data;
using Ico.Reader.Data.IcoSources;
using Ico.Reader.Data.Source;

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
    public IcoReader(IcoReaderConfiguration icoReaderConfiguration) => _icoReaderConfiguration = icoReaderConfiguration;

    /// <summary>
    /// Initializes a new instance of the icoReader class with default configuration settings.
    /// </summary>
    public IcoReader() => _icoReaderConfiguration = new IcoReaderConfiguration();

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
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 0, false);

        var IcoData = ReadFromStream(stream, icoSource);
        if (IcoData is null)
            return null;

        IcoData.Name = Path.GetFileNameWithoutExtension(filePath);
        return IcoData;
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

        var IcoData = ReadFromStream(stream, icoSource);
        if (IcoData is null)
            return null;

        return IcoData;
    }

    /// <summary>
    /// Reads ico data from a stream.
    /// </summary>
    /// <param name="stream">The stream containing the ico data.</param>
    /// <returns>An IcoData object containing the read ico data, or null if the data cannot be read.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the provided stream is null.</exception>
    public IcoData? Read(Stream stream)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        var icoSource = new StreamBufferSource(stream);
        var IcoData = ReadFromStream(stream, icoSource);
        if (IcoData is null)
            return null;

        return IcoData;
    }

    /// <summary>
    /// Reads ico data from a stream using a specified ico source.
    /// </summary>
    /// <param name="stream">The stream containing the ico data.</param>
    /// <param name="icoSource">The ico source providing the stream.</param>
    /// <returns>An IcoData object containing the read ico data, or null if the data cannot be read.</returns>
    private IcoData? ReadFromStream(Stream stream, IIcoSource icoSource)
    {
        IcoData? IcoData;
        if (_icoReaderConfiguration.IcoExeDecoder.IsPeFormat(stream))
            IcoData = ReadFromExe(stream, icoSource);
        else
            IcoData = ReadFromIco(stream, icoSource);

        if (IcoData is null)
            return null;

        return IcoData;
    }

    /// <summary>
    /// Reads ico data from an executable file stream.
    /// </summary>
    /// <param name="stream">The stream representing the executable file.</param>
    /// <param name="icoSource">The ico source providing the stream.</param>
    /// <returns>An IcoData object if ico data is successfully read, otherwise null.</returns>
    private IcoData? ReadFromExe(Stream stream, IIcoSource icoSource)
    {
        if (!_icoReaderConfiguration.IcoExeDecoder.IsPeFormat(stream))
            return null;

        var decodedicoResult = _icoReaderConfiguration.IcoExeDecoder.GetDecodedIcoResult(stream, _icoReaderConfiguration.IcoDecoder);
        if (decodedicoResult is null)
            return null;

        return new IcoData(_icoReaderConfiguration.IcoDecoder, icoSource, decodedicoResult);
    }

    private IcoData? ReadFromIco(Stream stream, IIcoSource icoSource)
    {
        var decodedicoResult = new DecodedIcoResult { OriginFileType = IcoOriginFileType.Ico, IcoGroups = new IcoGroup[1] };
        decodedicoResult.IcoGroups[0] = new IcoGroup() { Name = "1", Header = IcoHeader.ReadFromStream(stream) };

        if (decodedicoResult.IcoGroups[0].Header!.Reserved != 0)
        {
            stream.Dispose();
            return null;
        }

        if(decodedicoResult.IcoGroups[0].Header!.ImageType != 1)
        {
            decodedicoResult.OriginFileType = IcoOriginFileType.Cursor;
        }

        decodedicoResult.IcoGroups[0].DirectoryEntries = IcoDirectoryEntry.ReadEntriesFromStream(stream, decodedicoResult.IcoGroups[0].Header!);
        decodedicoResult.References = new ImageReference[decodedicoResult.IcoGroups[0].DirectoryEntries!.Length];
        for (var i = 0; i < decodedicoResult.IcoGroups[0].DirectoryEntries!.Length; i++)
        {
            if (decodedicoResult.IcoGroups[0].DirectoryEntries![i].Reserved != 0)
                return null;

            var imageReference = ImageReference.FromIcoDirectoryEntry(stream, decodedicoResult.IcoGroups[0].DirectoryEntries![i], _icoReaderConfiguration.IcoDecoder);
            if (imageReference is null)
                return null;

            imageReference.Id = i;
            decodedicoResult.References[i] = imageReference;
        }

        return new IcoData(_icoReaderConfiguration.IcoDecoder, icoSource, decodedicoResult);
    }
}
