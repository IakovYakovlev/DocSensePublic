namespace DocSenseV1.Services.AiProvider
{
    public class AiProviderService : IAiProviderService
    {
        private readonly IAiProviderFactory _factory;
        private readonly IConfiguration _config;
        private readonly string provider = string.Empty;

        public AiProviderService(IAiProviderFactory factory, IConfiguration config)
        {
            _factory = factory;
            _config = config;
            provider = _config["AiProvider"] ?? string.Empty;
        }


        public async Task<string> RequestAsync(string prompt)
        {
            IAiProviderStrategy strategy = await _factory.GetStrategyAsync(provider);
            var result = await strategy.ExecuteAsync(prompt);

            return result;
        }
    }
}
