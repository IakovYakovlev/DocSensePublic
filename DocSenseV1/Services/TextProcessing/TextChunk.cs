
using DocSenseV1.Dtos;
using Microsoft.Extensions.Options;

namespace DocSenseV1.Services.TextProcessing
{
    public class TextChunk : ITextChunk
    {
        private readonly TextProcessingConfig _textProcConfig;
        public TextChunk(IOptions<TextProcessingConfig> textProcConfig)
        {
            _textProcConfig = textProcConfig.Value;
        }

        public Task<List<string>> GetChunks(string text)
        {
            List<string> chunks = new List<string>();
            if (string.IsNullOrEmpty(text))
            {
                return Task.FromResult(chunks);
            }

            int chunkSize = _textProcConfig.ChunkSizeSymbols;
            int overlap = _textProcConfig.ChunkOverlapSymbols;

            int step = Math.Max(1, chunkSize - overlap);

            for(int start = 0; start < text.Length; start += step)
            {
                int length = Math.Min(chunkSize, text.Length - start);
                chunks.Add(text.Substring(start, length));

                // Если мы захватили остаток текста до самогу конца, выходим из цикла
                if (start + length >= text.Length) break;
            }

            //int start = 0;
            //while(start < text.Length)
            //{
            //    // 1. Берем подстроку от текущего старта
            //    int length = Math.Min(chunkSize, text.Length - start);
            //    chunks.Add(text.Substring(start, length));

            //    // 2. Если мы уже дошли до конца текста, цикл закончен
            //    if(start + length >= text.Length)
            //    {
            //        break;
            //    }

            //    // 3. Вычисляем следующий старт с учетом перекрытия
            //    int step = chunkSize - overlap;

            //    // Шаг должен быть положительным, чтобы избежать бесконечного цикла
            //    if(step <= 0)
            //    {
            //        step = 1;
            //    }

            //    start += step;
            //}

            //for(int i = 0; i < text.Length; i += chunkSize)
            //{
            //    int length = Math.Min(chunkSize, text.Length - i);

            //    string chunk = text.Substring(i, length);
            //    chunks.Add(chunk);
            //}

            return Task.FromResult(chunks);
        }
    }
}
