using DocSenseV1.Exceptions;
using DocSenseV1.Services.AiProvider;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocSenseV1Test.Services.AiProvider
{
    public class AiProviderFactoryTest
    {
        [Fact]
        public async Task GetStrategy_ShouldReturnCorrectStrategy_WhenSupported()
        {
            // Arrange
            string testProvider = "test provider";
            var mockStrategy = new Mock<IAiProviderStrategy>();
            mockStrategy.Setup(s => s.CanHandle(It.Is<string>(provider => 
                provider.Equals(testProvider, StringComparison.OrdinalIgnoreCase)))).Returns(true);

            var mockOtherStrategy = new Mock<IAiProviderStrategy>();

            var strategies = new List<IAiProviderStrategy>
            {
                mockStrategy.Object,
                mockOtherStrategy.Object,
            };

            var factory = new AiProviderFactory(strategies);

            // Act
            var result = await factory.GetStrategyAsync(testProvider);

            // Assert
            Assert.Equal(mockStrategy.Object, result);
        }

        [Fact]
        public async Task GetStrategy_ShouldThrowUnsupportedProviderException_WhenNotSupported()
        {
            // Arrange
            var strategies = Enumerable.Empty<IAiProviderStrategy>();
            var factory = new AiProviderFactory(strategies);

            // Act & Assert
            await Assert.ThrowsAsync<UnsupportedProviderException>(() => factory.GetStrategyAsync("test"));
        }
    }
}
