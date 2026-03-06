using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using System.Text;

namespace DocSenseV1.Services.FileReader.Strategies
{
    public class DocxFileReaderStrategy : IFileReaderStrategy
    {
        public bool CanHandle(string extension) => extension.Equals(".docx", StringComparison.OrdinalIgnoreCase);

        public Task<string> ReadTextAsync(Stream fileStream)
        {
            if(fileStream.Position > 0 && fileStream.CanSeek)
            {
                fileStream.Position = 0;
            }

            using var wordDoc = WordprocessingDocument.Open(fileStream, false);

            var body = wordDoc.MainDocumentPart?.Document.Body;
            var text = string.Empty;
            if(body != null)
            {
                text = body.InnerText;
            }

            return Task.FromResult(text.ToString());
        }
    }
}
