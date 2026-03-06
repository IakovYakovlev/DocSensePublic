namespace DocSenseV1.Services.AiProvider
{
    public interface IAiProviderService
    {
        Task<string> RequestAsync(string prompt);
    }
}
