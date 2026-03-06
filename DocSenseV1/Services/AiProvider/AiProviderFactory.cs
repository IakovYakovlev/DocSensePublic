using DocSenseV1.Exceptions;

namespace DocSenseV1.Services.AiProvider
{
    public class AiProviderFactory : IAiProviderFactory
    {
        private readonly IEnumerable<IAiProviderStrategy> _strategies;

        public AiProviderFactory(IEnumerable<IAiProviderStrategy> strategies)
        {
            _strategies = strategies;
        }

        public Task<IAiProviderStrategy> GetStrategyAsync(string provider)
        {
            IAiProviderStrategy? strategy = _strategies.FirstOrDefault(s => s.CanHandle(provider));

            if (strategy == null)
                throw new UnsupportedProviderException("Unsupported provider type.");

            return Task.FromResult(strategy);
        }
    }
}
