using UglyToad.PdfPig;
using System.Text;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace DocSenseV1.Services.FileReader.Strategies
{
    public class PdfFileReaderStrategy : IFileReaderStrategy
    {
        public bool CanHandle(string extension) => extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);

        public Task<string> ReadTextAsync(Stream fileStream)
        {
           using var document = PdfDocument.Open(fileStream);
            var text = new StringBuilder();

            foreach(var page in document.GetPages())
            {
                text.AppendLine(ContentOrderTextExtractor.GetText(page));
            }

            return Task.FromResult(text.ToString());
        }
    }
}
