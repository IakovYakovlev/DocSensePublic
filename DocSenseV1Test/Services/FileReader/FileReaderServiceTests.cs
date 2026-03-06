using DocSenseV1.Services.FileReader;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocSenseV1Test.Services.FileReader
{
    public class FileReaderServiceTests
    {
        [Theory]
        [InlineData("test.pdf", ".pdf", "PDF content")]
        [InlineData("report.docx", ".docx", "DOCX content")]
        [InlineData("log.txt", ".txt", "TXT content")]
        public async Task ReadFile_ShouldUseCorrectStrategy_BasedOnFileExtension(
            string testFileName,
            string expectedExtension,
            string expectedContent)
        {
            // Arrange
            var mockStrategy = new Mock<IFileReaderStrategy>();
            mockStrategy.Setup(s => s.ReadTextAsync(It.IsAny<Stream>()))
                .ReturnsAsync(expectedContent);

            var mockFactory = new Mock<IFileReaderFactory>();
            mockFactory.Setup(f => f.GetStrategy(expectedExtension))
                .Returns(mockStrategy.Object);

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns(testFileName);
            mockFile.Setup(f => f.Length).Returns(1);

            var streamContent = new MemoryStream();
            mockFile.Setup(f => f.OpenReadStream()).Returns(streamContent);

            var readService = new FileReaderService(mockFactory.Object);

            // Act
            var result = await readService.ReadFileAsync(mockFile.Object);

            // Assert
            Assert.Equal(expectedContent, result);
            mockFactory.Verify(f => f.GetStrategy(expectedExtension), Times.Once());
            mockStrategy.Verify(s => s.ReadTextAsync(It.IsAny<Stream>()), Times.Once());
        }

        [Fact]
        public async Task ReadFile_ShouldThrowNotSupportedException_WhenStrategyIsNotFound()
        {
            // Arrange
            var mockFactory = new Mock<IFileReaderFactory>();
            mockFactory.Setup(f => f.GetStrategy(".png"))
                .Throws(new NotSupportedException($"Unsupported file type. " +
                    $"Supported types are: .txt, .pdf, .docx. Received: .png"));

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("image.png");

            var readService = new FileReaderService(mockFactory.Object);

            // Act & Asseert
            await Assert.ThrowsAsync<NotSupportedException>(() => readService.ReadFileAsync(mockFile.Object));
        }

        [Fact]
        public async Task ReadFile_ShouldThrowNotSupportedException_WhenFileIsEmpty()
        {
            // Arrange
            var mockFactory = new Mock<IFileReaderFactory>();
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(0);
            var readService = new FileReaderService(mockFactory.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotSupportedException>(
                () => readService.ReadFileAsync(mockFile.Object));

            Assert.Equal("The uploaded file is empty", exception.Message);

            mockFactory.Verify(f => f.GetStrategy(It.IsAny<string>()), Times.Never);
        }
    }
}
