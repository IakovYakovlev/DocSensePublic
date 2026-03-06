namespace DocSenseV1.Services.AiProvider
{
    public interface IAiProviderStrategy
    {
        bool CanHandle(string aiProvider);
        Task<string> ExecuteAsync(string propt);
    }
}
