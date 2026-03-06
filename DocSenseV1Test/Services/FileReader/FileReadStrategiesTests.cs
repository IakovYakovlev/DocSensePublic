using DocSenseV1.Services.FileReader.Strategies;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocSenseV1Test.Services.FileReader
{
    public class FileReadStrategiesTests
    {
        [Fact]
        public async Task ReadTextAsyncDocx_ShouldExtractKnownTextFromDocx()
        {
            // Arrange
            var strategy = new DocxFileReaderStrategy();
            var testFilePath = Path.Combine(Directory.GetCurrentDirectory(), 
                "Services",
                "FileReader",
                "TestFiles",
                $"test_doc.docx");
            if(!File.Exists(testFilePath))
            {
                throw new FileNotFoundException($"Test file not found: {testFilePath}");
            }
            var expectedSubstring = "This document contains test docx data.";

            // Act
            using var fileStream = File.OpenRead(testFilePath);
            var result = await strategy.ReadTextAsync(fileStream);

            // Assert
            Assert.Contains(expectedSubstring, result);
            Assert.True(result.Length == expectedSubstring.Length);
        }

        [Fact]
        public async Task ReadTextAsyncPdf_ShouldExtractKnownTextFromDocx()
        {
            // Arrange
            var strategy = new PdfFileReaderStrategy();
            var testFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                "Services",
                "FileReader",
                "TestFiles",
                $"test_pdf.pdf");
            if (!File.Exists(testFilePath))
            {
                throw new FileNotFoundException($"Test file not found: {testFilePath}");
            }
            var expectedSubstring = "This document contains test pdf data.";

            // Act
            using var fileStream = File.OpenRead(testFilePath);
            var result = await strategy.ReadTextAsync(fileStream);

            // Assert
            Assert.Contains(expectedSubstring, result);
            Assert.True(result.Length > expectedSubstring.Length);
        }

        [Fact]
        public async Task ReadTextAsyncTxt_ShouldExtractKnownTextFromDocx()
        {
            // Arrange
            var strategy = new TextFileReaderStrategy();
            var testFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                "Services",
                "FileReader",
                "TestFiles",
                $"test_txt.txt");
            if (!File.Exists(testFilePath))
            {
                throw new FileNotFoundException($"Test file not found: {testFilePath}");
            }
            var expectedSubstring = "This document contains test txt data.";

            // Act
            using var fileStream = File.OpenRead(testFilePath);
            var result = await strategy.ReadTextAsync(fileStream);

            // Assert
            Assert.Contains(expectedSubstring, result);
            Assert.True(result.Length == expectedSubstring.Length);
        }
    }
}
