
using DocSenseV1.Dtos;
using DocSenseV1.Models;
using DocSenseV1.Repositories.Job;
using DocSenseV1.Services.AiProvider;
using GenerativeAI.Types;
using Microsoft.Extensions.Caching.Distributed;
using Polly;
using Polly.Retry;
using System.Diagnostics;

namespace DocSenseV1.Services.TextProcessing
{
    public class TextProcessingService : ITextProcessingService
    {
        private readonly ITextChunk _textChunk;
        private readonly IAiProviderService _aiProviderService;
        private readonly IDistributedCache _cache;
        private readonly IJobRepository _jobRepository;
        private readonly ILogger<TextProcessingService> _logger;

        private readonly AsyncRetryPolicy<string> _analyzeRetryPolicy;
        private readonly AsyncRetryPolicy<string> _reduceRetryPolicy;
        private static readonly Random _random = new Random();

        public TextProcessingService(
            ITextChunk textChunk, 
            IAiProviderService aiProviderService,
            IDistributedCache cache,
            IJobRepository jobRepository,
            ILogger<TextProcessingService> logger,
            AsyncRetryPolicy<string>? analyzePolicy = null,
            AsyncRetryPolicy<string>? reducePolicy = null
            )
        {
            _textChunk = textChunk;
            _aiProviderService = aiProviderService;
            _cache = cache;
            _jobRepository = jobRepository;
            _logger = logger;

            _analyzeRetryPolicy = analyzePolicy ?? Policy<string>
                .Handle<Exception>()
                .WaitAndRetryAsync(3, attempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, attempt)) // 2, 4, 8
                    + TimeSpan.FromMilliseconds(_random.Next(0, 1000)));

            _reduceRetryPolicy = reducePolicy ?? Policy<string>
                .Handle<Exception>()
                .WaitAndRetryAsync(5, attemp =>
                    TimeSpan.FromSeconds(Math.Pow(2, attemp)) // 2, 4, 8, 16, 32
                    + TimeSpan.FromMilliseconds(_random.Next(0, 1000)));
        }

        public async Task<string> ProcessAsync(string text, string jobId, CancellationToken ct = default)
        {
            // Основная задача - отправить текст в LLM и вернуть правильный ответ.
            var sw = Stopwatch.StartNew();

            var job = await _jobRepository.GetJobByIdAsync(jobId);
            if (job == null)
            {
                throw new Exception($"Job with ID {jobId} not found in database. Cannot start processing.");
            }

            job.Status = "Processing";
            job.SymbolsCount = text.Length;
            await _jobRepository.UpdateAsync(job);


            string finalResult = string.Empty;

            // Перед отправкой надо :
            // разделить текст на отдельные части.
            List<string>? chunks = null;
            ChunkProcessingResult[]? results = null;

            try
            {
                chunks = await _textChunk.GetChunks(text);

                if(chunks == null || chunks.Count == 0)
                {
                    throw new Exception("No text chunks were generated. " +
                        "The input text might be too short or invalid.");
                }

                job.TotalChunks = chunks.Count;
                // Отправляем каждый чанк на обработку в LLM, дожидаемся ответа, если ответ ошибка, 
                using var semaphore = new SemaphoreSlim(3); // Ограничение на 3 одновременных запроса

                // Если есть хоть одна ошибка - пытаемся обработать чанки до 3 раз
                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    // Выбрасывает OperationCanceledException, если токен отменен
                    ct.ThrowIfCancellationRequested();

                    var tasks = new List<Task<ChunkProcessingResult>>();

                    for(int i = 0; i < chunks.Count; i++)
                    {
                        var chunk = chunks[i];
                        var index = i;

                        tasks.Add(Task.Run(async () =>
                            await GetFromCacheAsync(jobId, index) ?? 
                                await ProcessWithAiAsync(jobId, chunk, index, semaphore, ct)));

                        if(i < chunks.Count - 1)
                            await Task.Delay(700, ct); // Небольшая пауза между запусками батчей
                    }

                    results = await Task.WhenAll(tasks);

                    // Если всё успешно - прерываем цикл попыток
                    if (results.All(r => r.IsSuccess))
                        break;

                    // Если не всё успешно и это не последняя попытка - добавляем паузу
                    // Экспоненциальный backoff: 1-я попытка → 2 сек, 2-я → 4 сек, 3-я → 8 сек
                    // + jitter (0-1000ms) для избежания thundering herd problem
                    if (attempt < 3)
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))
                            + TimeSpan.FromMilliseconds(_random.Next(0, 1000)), 
                            ct);

                }
                
                if(results != null)
                {
                    finalResult = await ReduceResultsAsync(results, ct);
                }

                int successCount = results?.Count(r => r.IsSuccess) ?? 0;

                if (successCount == 0)
                    throw new Exception("All text chunks failed to process after multiple attempts.");

                if (successCount == chunks.Count)
                    job.Status = "Completed";
                else
                {
                    job.Status = "PartialSuccess";
                    job.Error = "Some chunks failed to process.";
                }

                job.Result = finalResult;
                job.ResultSymbolsCount = finalResult.Length;
            }
            catch(OperationCanceledException)
            {
                job.Status = "Cancelled";
                job.Error = "Processing was cancelled by user or system.";
                throw;
            }
            catch(Exception ex)
            {
                job.Status = "Failed";
                job.Error = ex.Message;
                throw;
            }
            finally
            {
                sw.Stop();
                job.ExecutionTimeMs = sw.Elapsed.TotalMilliseconds;

                if(results != null)
                {
                    job.SuccessfulChunksCount = results.Count(r => r.IsSuccess);
                    job.FailedChunksCount = results.Count(r => !r.IsSuccess);
                }

                await _jobRepository.UpdateAsync(job);

                await ClearCacheAsync(jobId, chunks?.Count ?? 0);
            }

            // Это предложил LLM gemini
            /*
             2. Нужно ли говорить клиенту?
                Тут есть два подхода:

                Прозрачный: Добавить в JSON ответа поле isComplete: false или confidence: 0.95. 
                Это честно, если вы делаете серьезный аналитический инструмент.

                Бесшовный: Просто вернуть текст. Если чанков много, пользователь может даже 
                не заметить отсутствия маленького кусочка данных.

                3. Логирование для разработчика (PostgreSQL)
                Чтобы вы как разработчик знали, что система работает «на костылях», в БД в 
                таблицу биллинга/логов мы записываем:

                status: "PartialSuccess" (вместо "Success").

                failed_chunks_indexes: "[7, 12]" (список индексов, которые не прошли).

                error_messages: "Gemini quota exceeded / Safety filter block".
             */

            return finalResult;
        }

        private async Task ClearCacheAsync(string jobId, int chunksCount)
        {
            if (chunksCount <= 0) return;

            try
            {
                var cleanupTasks = Enumerable.Range(0, chunksCount)
                    .Select(index => _cache.RemoveAsync($"chunk:{jobId}:{index}", default));

                await Task.WhenAll(cleanupTasks);
            }
            catch(Exception ex)
            {
                // Здесь мы просто логируем ошибку, но не "ломаем" основной поток.
                // Очистка кэша не должна быть критической точкой отказа.
                _logger.LogWarning(ex, "Failed to clear cache for job {JobId}", jobId);
            }
        }

        private async Task<string> AnalyzeText(string text, CancellationToken ct)
        {
            string prompt = $@"Analyze the following text and return a JSON object. 
                IMPORTANT: All text values (summary, keywords, main_topics, insights) MUST be in the same language as the input text provided below.

                The JSON object must contain:
                - summary (a brief overview)
                - keywords (a list of important terms)
                - sentiment (positive/negative/neutral)
                - main_topics (a list of key themes)
                - insights (a list of deep observations)

                Respond ONLY in valid JSON format.

                Text:'''{text}'''
            ";

            return await _analyzeRetryPolicy.ExecuteAsync(async (token) =>
                await _aiProviderService.RequestAsync(prompt), ct);
        }

        private async Task<string> ReduceResultsAsync(IEnumerable<ChunkProcessingResult> results, CancellationToken ct)
        {
            // Берем только успешные ответы
            var successfulResponses = results
                .Where(r => r.IsSuccess)
                .OrderBy(r => r.Index)
                .Select(r => r.Response)
                .ToList();

            if (!successfulResponses.Any()) return string.Empty;
            if (successfulResponses.Count == 1) return successfulResponses[0]!;

            // Формируем промпт для объединения
            string combinedSections = string.Join("\n\n", successfulResponses.Select((res, i) => $"// Section {i + 1}\n{res}"));

            string mergePrompt = $@"
                You are an AI assistant. Your task is to synthesize analysis results from different sections of a document into one final, high-quality report.

                REQUIREMENTS:
                1. LANGUAGE: All output values (summary, keywords, etc.) MUST be in the same language as the input sections provided below.
                2. SUMMARY: Create a single, cohesive, and well-structured summary. Synthesize the information into a logical narrative.
                3. KEYWORDS & TOPICS: Merge lists, remove duplicates.
                4. INSIGHTS: Combine and refine insights.
                5. FORMAT: Return ONLY valid JSON. No additional text.

                Input Sections JSON:
                {combinedSections}

                Return the final synthesized JSON.";

            return await _reduceRetryPolicy.ExecuteAsync(async (token) =>
                await _aiProviderService.RequestAsync(mergePrompt), ct);
        }

        private async Task<ChunkProcessingResult?> GetFromCacheAsync(string jobId, int index)
        {
            string cacheKey = GetCacheKey(jobId, index);

            var cachedResponse = await _cache.GetStringAsync(cacheKey);

            return string.IsNullOrEmpty(cachedResponse)
                ? null
                : new ChunkProcessingResult
                {
                    Index = index,
                    Response = cachedResponse,
                    IsSuccess = true
                };
        }

        private async Task<ChunkProcessingResult> ProcessWithAiAsync(string jobId, string chunk, int index, SemaphoreSlim semaphore, CancellationToken ct)
        {
            await semaphore.WaitAsync();
            try
            {
                string response = await AnalyzeText(chunk, ct);
                string cacheKey = GetCacheKey(jobId, index);

                // Настройки кэширования
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    // Данные удалятся через 10 минут после записи
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                };

                await _cache.SetStringAsync(cacheKey, response, cacheOptions);

                return new ChunkProcessingResult
                { Index = index, Response = response, IsSuccess = true };
            }
            catch (Exception ex)
            {
                return new ChunkProcessingResult
                { Index = index, ErrorMessage = ex.Message, IsSuccess = false };
            }
            finally
            {
                semaphore.Release();
            }
        }

        private string GetCacheKey(string jobId, int index) => $"chunk:{jobId}:{index}";
    }
}
