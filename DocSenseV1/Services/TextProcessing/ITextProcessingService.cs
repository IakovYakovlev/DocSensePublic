namespace DocSenseV1.Services.TextProcessing
{
    public interface ITextProcessingService
    {
        Task<string> ProcessAsync(string text, string jobId, CancellationToken ct = default);
    }
}
