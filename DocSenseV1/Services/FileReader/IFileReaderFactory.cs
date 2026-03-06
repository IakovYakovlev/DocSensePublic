namespace DocSenseV1.Services.FileReader
{
    public interface IFileReaderFactory
    {
        public IFileReaderStrategy GetStrategy(string extension);
    }
}
