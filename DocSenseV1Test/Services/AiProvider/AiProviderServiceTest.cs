using DocSenseV1.Services.AiProvider;
using Microsoft.Extensions.Configuration;
using Moq;

namespace DocSenseV1Test.Services.AiProvider
{
    public class AiProviderServiceTest
    {
        [Fact]
        public async Task RequesAsync_ShouldReturnResultFromAsync()
        {
            // Arrange
            string expectedProvider = "Gemini";
            string prompt = "some text";
            string expectedResponse = "{\"status\": \"ok\"}";
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["AiProvider"]).Returns(expectedProvider);

            var strategyMock = new Mock<IAiProviderStrategy>();

            var factoryMock = new Mock<IAiProviderFactory>();
            factoryMock.Setup(f => f.GetStrategyAsync(expectedProvider)).ReturnsAsync(strategyMock.Object);

            strategyMock.Setup(s => s.ExecuteAsync(prompt)).ReturnsAsync(expectedResponse);

            IAiProviderService provider = new AiProviderService(factoryMock.Object, configMock.Object);

            // Act
            var result = await provider.RequestAsync(prompt);

            // Assert
            Assert.NotNull(result);

            factoryMock.Verify(f => f.GetStrategyAsync(It.IsAny<string>()), Times.Once);

            strategyMock.Verify(s => s.ExecuteAsync(It.IsAny<string>()), Times.Once);
        }
    }
}
