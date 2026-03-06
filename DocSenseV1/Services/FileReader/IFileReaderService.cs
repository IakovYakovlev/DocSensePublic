namespace DocSenseV1.Services.FileReader
{
    public interface IFileReaderService
    {
        public Task<string> ReadFileAsync(IFormFile file);
    }
}
