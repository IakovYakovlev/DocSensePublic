namespace DocSenseV1.Services.FileReader
{
    public interface IFileReaderStrategy
    {
        bool CanHandle(string extension);
        Task<string> ReadTextAsync(Stream fileStream);
    }
}
