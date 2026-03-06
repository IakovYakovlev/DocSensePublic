namespace DocSenseV1.Services.FileReader.Strategies
{
    public class TextFileReaderStrategy : IFileReaderStrategy
    {
        public bool CanHandle(string extension) => extension.Equals(".txt", StringComparison.OrdinalIgnoreCase);

        public async Task<string> ReadTextAsync(Stream fileStream)
        {
            using var reader = new StreamReader(fileStream);
            return await reader.ReadToEndAsync();
        }
    }
}
