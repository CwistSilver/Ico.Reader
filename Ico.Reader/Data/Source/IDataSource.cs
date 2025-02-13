namespace Ico.Reader.Data.IcoSources;
public interface IDataSource
{
    Stream GetStream(bool useAsync = false);
}
