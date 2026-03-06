using DocSenseV1.Services.AiProvider.Strategies;
using Microsoft.Extensions.Configuration;
using Moq;

namespace DocSenseV1Test.Services.AiProvider
{
    public class AiProviderStrategiesTest
    {
        private readonly Mock<IConfiguration> _config;

        public AiProviderStrategiesTest()
        {
            _config = new Mock<IConfiguration>();
        }

        [Fact]
        public void CanHandle_ShouldReturnTrue_WhenProviderIsGemini()
        {
            // Arrange
            _config.Setup(x => x[It.IsAny<string>()]).Returns("anyValue");
            var provider = new GeminiProvider(_config.Object);

            // Act
            var result = provider.CanHandle("Gemini");
            var resultLower = provider.CanHandle("gemini");

            // Assert
            Assert.True(result);
            Assert.True(resultLower);
        }

        [Fact]
        public void Constructor_ShouldThrowException_WhenApiKeyIsMissing()
        {
            // Arrange
            _config.Setup(x => x["GeminiApiKey"]).Returns((string?)null);

            // Act
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new GeminiProvider(_config.Object));

            // Assert
            Assert.Contains("GeminiProvider API Key is missing", exception.Message);
        }

        // Интеграционный тест (требует реальный ключ в secrets.json или окружении)
        [Fact(Skip = "Требуется реальный API ключ")]
        //[Fact]
        public async Task ExecuteAsync_ShouldReturnCleanJson_WhenPromptIsSent()
        {
            // Arrange
            _config.Setup(k => k["GeminiApiKey"]).Returns("real_api_key_here");
            _config.Setup(k => k["GeminiModelName"]).Returns("gemini-2.5-flash");
            var strategy = new GeminiProvider(_config.Object);
            var prompt = "Верни JSON объект с полем 'status' и значением 'ok'";

            // Act
            var result = await strategy.ExecuteAsync(prompt);

            // Assert
            // 1. Проверяем наличие ключевого поля
            Assert.Contains("\"status\"", result);

            // 2. Проверяем, что строка НЕ начинается с разметки Markdown
            Assert.False(result.StartsWith("```json"), "Ответ не должен содержать Markdown разметку");

            // 3. Проверяем структуру JSON
            string trimmedResult = result.Trim();
            Assert.True(trimmedResult.StartsWith("{") && trimmedResult.EndsWith("}"),
                "Ответ должен быть вилидным JSON объектом");
        }
    }
}
