namespace DocSenseV1.Services.AiProvider
{
    public interface IAiProviderFactory
    {
        Task<IAiProviderStrategy> GetStrategyAsync(string provider);
    }
}
