using GenerativeAI;
using System.Text.RegularExpressions;

namespace DocSenseV1.Services.AiProvider.Strategies
{
    public class GeminiProvider : IAiProviderStrategy
    {
        private readonly string _apiKey = string.Empty;
        private readonly string _modelName = string.Empty;

        public GeminiProvider(IConfiguration config)
        {
            _apiKey = config["GeminiApiKey"] ?? throw new ArgumentNullException("GeminiProvider API Key is missing");
            _modelName = config["GeminiModelName"] ?? throw new ArgumentNullException("Gemini model name is missing");
        }

        public bool CanHandle(string aiProvider) => aiProvider.Equals("Gemini", StringComparison.OrdinalIgnoreCase);

        public async Task<string> ExecuteAsync(string propt)
        {
            // Инициализация клиента
            var client = new GenerativeModel(_apiKey, _modelName);

            // Выполенение запроса
            var response = await client.GenerateContentAsync(propt);
            string rawText = response.Text() ?? string.Empty;

            // Очистка от Markdown JSON
            return ExtractJson(rawText);
        }

        private string ExtractJson(string input)
        {
            if(string.IsNullOrWhiteSpace(input)) return "{}";

            var match = Regex.Match(input, @"\{[\s\S]*\}");
            return match.Success ? match.Value : input;
        }
    }
}
