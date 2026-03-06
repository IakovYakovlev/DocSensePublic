using DocSenseV1.Exceptions;
using DocSenseV1.Services.FileReader;
using DocSenseV1.Services.FileReader.Strategies;
using Moq;

namespace DocSenseV1Test.Services.FileReader
{
    public class FileReaderFactoryTests
    {
        [Theory]
        [InlineData(".pdf")]
        [InlineData(".docx")]
        [InlineData(".txt")]
        [InlineData(".PDF")]
        public async Task GetStrategy_ShouldReturnCorrectStrategy_WhenSupported(string extension)
        {
            // Arrange
            var mockStrategy = new Mock<IFileReaderStrategy>();
            mockStrategy.Setup(s => s.CanHandle(It.Is<string>(ext =>
                ext.Equals(extension, StringComparison.OrdinalIgnoreCase)
                ))).Returns(true);

            var mockOtherStrategy = new Mock<IFileReaderStrategy>();

            var strategies = new List<IFileReaderStrategy> 
                { mockStrategy.Object, mockOtherStrategy.Object };
            var factory = new FileReaderFactory(strategies);

            // Act
            var result = factory.GetStrategy(extension);

            // Aseert
            Assert.Equal(mockStrategy.Object, result);
        }

        [Fact]
        public void GetStrategy_ShouldThrowUnsupportedFileException_WhenNotSupported()
        {
            // Arrange
            var strategies = Enumerable.Empty<IFileReaderStrategy>();
            var factory = new FileReaderFactory(strategies);

            // Act & Assert
            Assert.Throws<UnsupportedFileException>(() => factory.GetStrategy(".png"));
        }
    }
}
