namespace DocSenseV1.Dtos
{
    public class TextProcessingConfig
    {
        public int ChunkSizeSymbols { get; set; } = 10000;
        public int ChunkOverlapSymbols { get; set; } = 800;
    }
}
