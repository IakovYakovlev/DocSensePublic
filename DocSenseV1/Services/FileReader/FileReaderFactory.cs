using DocSenseV1.Exceptions;

namespace DocSenseV1.Services.FileReader
{
    public class FileReaderFactory : IFileReaderFactory
    {
        private readonly IEnumerable<IFileReaderStrategy> _strategies;

        public FileReaderFactory(IEnumerable<IFileReaderStrategy> strategies)
        {
            _strategies = strategies;
        }

        public IFileReaderStrategy GetStrategy(string extension)
        {
            var strategy = _strategies.FirstOrDefault(s => s.CanHandle(extension));
            if (strategy == null)
                throw new UnsupportedFileException($"Unsupported file type. " +
                    $"Supported types are: .txt, .pdf, .docx. Received: {extension}");

            return strategy;
        }
    }
}
