namespace Ico.Reader.Data.Source;

public interface IDataSource
{
    Stream GetStream(bool useAsync = false);
}
