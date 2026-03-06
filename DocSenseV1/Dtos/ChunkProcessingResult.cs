namespace DocSenseV1.Dtos
{
    public class ChunkProcessingResult
    {
        public int Index { get; set; }
        public string? Response { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public int TokensUsed { get; set; }
    }
}
