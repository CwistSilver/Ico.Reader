namespace Ico.Reader.Data.IcoSources;
internal interface IIcoSource
{    
   Stream GetStream(bool useAsync = false);
}
