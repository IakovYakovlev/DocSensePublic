namespace DocSenseV1.Services.TextProcessing
{
    public interface ITextChunk
    {
        Task<List<string>> GetChunks(string text);
    }
}
