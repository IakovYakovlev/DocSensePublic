using DocSenseV1.Dtos;
using DocSenseV1.Services.TextProcessing;
using Microsoft.Extensions.Options;

namespace DocSenseV1Test.Services.TextProcessing
{
    public class TextChunkerTest
    {
        private const int TestChunkSize = 1000;
        private readonly IOptions<TextProcessingConfig> _configOptions;

        public TextChunkerTest()
        {
            _configOptions = Options.Create(new TextProcessingConfig
            {
                ChunkSizeSymbols = TestChunkSize
            });
        }

        [Fact]
        public async Task GetChunks_ReturnSingelChunk_IfTextIsSmallerThanChunkSize()
        {
            // Arrange
            string text = "Small text";
            ITextChunk chunker = new TextChunk(_configOptions);

            // Act
            List<string> result = await chunker.GetChunks(text);

            // Assert
            Assert.Single(result);
            Assert.Equal(text, result.First());
        }

        [Fact]
        public async Task GetChunks_SplitTextIntoCorrectNumberOfChunks()
        {
            // Arrange
            // Размер 100, Перекрытие 50. 
            // Текст 210 символов.
            // Чанк 1: 0-100
            // Чанк 2: 50-150 (сдвиг на 100-50=50)
            // Чанк 3: 100-200
            // Чанк 4: 150-210
            const int chunkSize = 100;
            const int overlap = 50;
            var config = Options.Create(new TextProcessingConfig
            {
                ChunkSizeSymbols = chunkSize,
                ChunkOverlapSymbols = overlap
            });

            string text = new string('X', 210);
            ITextChunk chunker = new TextChunk(config);

            // Act
            List<string> result = await chunker.GetChunks(text);

            // Assert
            // Должно быть 4 чанка
            Assert.Equal(4, result.Count);
        }

        [Fact]
        public async Task GetChunks_ReturnEpmtyList_ForEmptyText()
        {
            // Arrang
            string text = string.Empty;
            ITextChunk chunker = new TextChunk(_configOptions);

            // Act
            List<string> result = await chunker.GetChunks(text);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetChunks_WithOverlap_SecondChunkShouldStartWithEndOfFirstChunk()
        {
            // Arrange
            const int chunkSize = 10;
            const int overlapSize = 2;
            var config = Options.Create(new TextProcessingConfig
            {
                ChunkSizeSymbols = chunkSize,
                ChunkOverlapSymbols = overlapSize
            });

            // Текст ровно на 2 чанка с учетом перекрытия
            // 0123456789 - первый (10)
            //        8901234567 - второй (начинается с индекса 8, т.е. "89" + еще 8 символов)
            string text = "0123456789ABCDEFGH";
            ITextChunk chunker = new TextChunk(config);

            // Act
            List<string> result = await chunker.GetChunks(text);

            // Assert
            Assert.Equal(2, result.Count);

            // Проверяем первый чанк
            Assert.Equal("0123456789", result[0]);

            // Проверяем второй чанк: он должен начинаться с "89" (последние 2 символа первого)
            Assert.StartsWith("89", result[1]);
            Assert.Equal("89ABCDEFGH", result[1]);
        }
    }
}
