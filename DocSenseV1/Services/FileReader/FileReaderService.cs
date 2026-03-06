using DocumentFormat.OpenXml.Office2016.Excel;

namespace DocSenseV1.Services.FileReader
{
    public class FileReaderService : IFileReaderService
    {
        private readonly IFileReaderFactory _factory;
        public FileReaderService(IFileReaderFactory factory)
        {
            _factory = factory;
        }
        public async Task<string> ReadFileAsync(IFormFile file)
        {
            if (file.Length == 0)
                throw new NotSupportedException("The uploaded file is empty");

            string ext = Path.GetExtension(file.FileName);
            IFileReaderStrategy reader = _factory.GetStrategy(ext);

            using var stream = file.OpenReadStream();

            return await reader.ReadTextAsync(stream);
        }
    }
}
