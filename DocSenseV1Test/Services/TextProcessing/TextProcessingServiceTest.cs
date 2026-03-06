using Castle.Core.Logging;
using DocSenseV1.Models;
using DocSenseV1.Repositories.Job;
using DocSenseV1.Services.AiProvider;
using DocSenseV1.Services.TextProcessing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using UglyToad.PdfPig.Graphics.Operations.TextShowing;

namespace DocSenseV1Test.Services.TextProcessing
{
    public class TextProcessingServiceTest
    {
        private Mock<ILogger<TextProcessingService>> CreateLoggerMock()
        {
            return new Mock<ILogger<TextProcessingService>>();
        }

        [Fact]
        public async Task ProcessAsync_CallShouldChank_Success()
        {
            // Arrange
            var jobId = Guid.NewGuid().ToString();
            string text = "some test text";
            var chunkMock = new Mock<ITextChunk>();
            chunkMock.Setup(c => c.GetChunks(It.IsAny<string>()))
                .ReturnsAsync(new List<string> { "chunk1" });

            var aiProviderServiceMock = new Mock<IAiProviderService>();
            aiProviderServiceMock.Setup(a => a.RequestAsync(It.IsAny<string>()))
                .ReturnsAsync("Ai result");

            var cacheMock = new Mock<IDistributedCache>();
            var jobRepositoryMock = new Mock<IJobRepository>();
            jobRepositoryMock.Setup(j => j.GetJobByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new JobModel { Id = jobId });

            var loggerMock = CreateLoggerMock();

            ITextProcessingService textProcessing = new TextProcessingService(
                chunkMock.Object, 
                aiProviderServiceMock.Object, 
                cacheMock.Object,
                jobRepositoryMock.Object,
                loggerMock.Object);


            // Act
            var result = await textProcessing.ProcessAsync(text, jobId);

            // Assert
            Assert.NotNull(result);

            chunkMock.Verify(c => c.GetChunks(It.IsAny<string>()), Times.Once);

            aiProviderServiceMock.Verify(c => c.RequestAsync(It.IsAny<string>()), Times.Once());
        }

        [Fact]
        public async Task ProcessAsync_ShouldSetFailedStatus_WhenExceptionOccurs()
        {
            // Arrange
            var jobId = Guid.NewGuid().ToString();
            var chunkMock = new Mock<ITextChunk>();
            chunkMock.Setup(c => c.GetChunks(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Chunking error"));

            var aiProviderServiceMock = new Mock<IAiProviderService>();
            var cacheMock = new Mock<IDistributedCache>();
            var jobRepositoryMock = new Mock<IJobRepository>();
            jobRepositoryMock.Setup(j => j.GetJobByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new JobModel { Id = jobId });

            var loggerMock = CreateLoggerMock();

            var service = new TextProcessingService(
                chunkMock.Object, aiProviderServiceMock.Object, cacheMock.Object, jobRepositoryMock.Object, loggerMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => service.ProcessAsync("test text", jobId));

            // Проверяем, что несмотря на ошибку, UpdateAsync был вызван со статусом Failed
            jobRepositoryMock.Verify(j => j.UpdateAsync(It.Is<JobModel>(job =>
                job.Id == jobId &&
                job.Status == "Failed" &&
                job.ExecutionTimeMs > 0)), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ProcessAsync_RetryOnFailure_CallsRequestMultipleTimes()
        {
            // Arrange
            var jobId = Guid.NewGuid().ToString();
            var chunkMock = new Mock<ITextChunk>();
            chunkMock.Setup(c => c.GetChunks(It.IsAny<string>()))
                .ReturnsAsync(new List<string> { "chunk1" });

            var aiProviderServiceMock = new Mock<IAiProviderService>();
            aiProviderServiceMock.SetupSequence(a => a.RequestAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("First fail"))   // 1-я попытка
                .ThrowsAsync(new Exception("Second fail"))  // 2-я попытка
                .ReturnsAsync("Success after retries");     // 3-я попытка (успех)

            var cacheMock = new Mock<IDistributedCache>();
            var jobRepositoryMock = new Mock<IJobRepository>();
            jobRepositoryMock.Setup(j => j.GetJobByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new JobModel { Id = jobId });

            var fastAnalyzePolicy = Policy<string>
                .Handle<Exception>()
                .WaitAndRetryAsync(3, _ => TimeSpan.Zero);

            var fastReducePolicy = Policy<string>
                .Handle<Exception>()
                .WaitAndRetryAsync(5, _ => TimeSpan.Zero);

            var loggerMock = CreateLoggerMock();

            var service = new TextProcessingService(
                chunkMock.Object, 
                aiProviderServiceMock.Object, 
                cacheMock.Object, 
                jobRepositoryMock.Object,
                loggerMock.Object,
                fastAnalyzePolicy,
                fastReducePolicy);

            // Act
            await service.ProcessAsync("test text", jobId);

            // Assert
            // Проверяем, что из-за политики Polly было ровно 3 вызова
            aiProviderServiceMock.Verify(a => a.RequestAsync(It.IsAny<string>()), Times.Exactly(3));
        }

        [Fact]
        public async Task ProcessAsync_OnSuccess_ShouldSaveToCache()
        {
            // Arrange
            var jobId = Guid.NewGuid().ToString();
            var chunkMock = new Mock<ITextChunk>();
            chunkMock.Setup(c => c.GetChunks(It.IsAny<string>()))
                .ReturnsAsync(new List<string> { "chunk1" });
            var aiProviderServiceMock = new Mock<IAiProviderService>();
            aiProviderServiceMock.Setup(a => a.RequestAsync(It.IsAny<string>()))
                .ReturnsAsync("Ai Response");

            var cacheMock = new Mock<IDistributedCache>();
            var jobRepositoryMock = new Mock<IJobRepository>();
            jobRepositoryMock.Setup(j => j.GetJobByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new JobModel { Id = jobId });

            var loggerMock = CreateLoggerMock();

            // Передаем мок кэша в сервис
            var service = new TextProcessingService
                (chunkMock.Object, aiProviderServiceMock.Object, cacheMock.Object, jobRepositoryMock.Object, loggerMock.Object);

            // Act
            await service.ProcessAsync("some text", jobId);

            // Assert
            // Проверяем, что кэш пытался сохранить результат
            cacheMock.Verify(x => x.SetAsync(
                    It.Is<string>(s => s.Contains("0")),
                    It.IsAny<byte[]>(),
                    It.IsAny<DistributedCacheEntryOptions>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessAsync_IfCacheExists_ShouldNotCallAiProvider()
        {
            // Arrange
            var jobId = "job-with-cache";
            string cachedResponse = "Cached AI Result";
            var chunkMock = new Mock<ITextChunk>();
            chunkMock.Setup(c => c.GetChunks(It.IsAny<string>()))
                .ReturnsAsync(new List<string> { "chunk1" });

            var aiProviderServiceMock = new Mock<IAiProviderService>();
            var cacheMock = new Mock<IDistributedCache>();

            // Настраиваем кэш так, будто там уже лежит ответ
            // Нам нужно замокать GetAsync, так как GetStringAsync - это метод расширения
            cacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), default))
                .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(cachedResponse));
            var jobRepositoryMock = new Mock<IJobRepository>();
            jobRepositoryMock.Setup(j => j.GetJobByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new JobModel { Id = jobId });

            var loggerMock = CreateLoggerMock();

            var service = new TextProcessingService(
                chunkMock.Object, aiProviderServiceMock.Object, cacheMock.Object, jobRepositoryMock.Object, loggerMock.Object);

            // Act
            var result = await service.ProcessAsync("some text", jobId);


            // Assert
            // 1. Проверяем, что AI провайдер НЕ вызывался
            aiProviderServiceMock.Verify(a => a.RequestAsync(It.IsAny<string>()), Times.Never);

            // 2. Проверяем, что в результате именно данные из кэша
            // (позже проверим это через DTO, пока просто убеждаемся, что тест упал)
        }

        [Fact]
        public async Task ProcessAsync_ShouldUseJobIdInCacheKey()
        {
            // Arrange
            var chunkMock = new Mock<ITextChunk>();
            chunkMock.Setup(c => c.GetChunks(It.IsAny<string>()))
                .ReturnsAsync(new List<string> { "chunk1" });

            var aiProviderServiceMock = new Mock<IAiProviderService>();
            aiProviderServiceMock.Setup(p => p.RequestAsync(It.IsAny<string>()))
                .ReturnsAsync("AI Response");

            var cacheMock = new Mock<IDistributedCache>();
            var jobRepositoryMock = new Mock<IJobRepository>();

            var loggerMock = CreateLoggerMock();

            var service = new TextProcessingService(
                chunkMock.Object, aiProviderServiceMock.Object, cacheMock.Object, jobRepositoryMock.Object, loggerMock.Object);

            string jobId1 = "job-1";
            jobRepositoryMock.Setup(j => j.GetJobByIdAsync(jobId1))
                .ReturnsAsync(new JobModel { Id = jobId1 });
            string jobId2 = "job-2";
            jobRepositoryMock.Setup(j => j.GetJobByIdAsync(jobId2))
                .ReturnsAsync(new JobModel { Id = jobId2 });

            // Act
            await service.ProcessAsync("text", jobId1);
            await service.ProcessAsync("text", jobId2);

            // Assert
            // Проверяем, что SetAsync вызывался с ключом, содержащим jobId1
            cacheMock.Verify(x => x.SetAsync(It.Is<string>(key => key.Contains(jobId1)),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
                Times.Once());

            // Проверяем, что SetAsync вызывался с ключом, содержащим jobId2
            cacheMock.Verify(x => x.SetAsync(It.Is<string>(key => key.Contains(jobId2)),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [Fact]
        public async Task ProcessAsync_MultipleChunks_ShouldCallAiToReduce()
        {
            // Arrange
            var jobId = Guid.NewGuid().ToString();
            var chunkMock = new Mock<ITextChunk>();

            // Имитируем разделение на 2 чанка
            chunkMock.Setup(c => c.GetChunks(It.IsAny<string>()))
                .ReturnsAsync(new List<string> { "chunk1", "chunk2" });

            var aiProviderServiceMock = new Mock<IAiProviderService>();
            aiProviderServiceMock.Setup(a => a.RequestAsync(It.IsAny<string>()))
                .ReturnsAsync("Reduced Result");

            var cacheMock = new Mock<IDistributedCache>();
            var jobRepositoryMock = new Mock<IJobRepository>();
            jobRepositoryMock.Setup(j => j.GetJobByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new JobModel { Id = jobId });

            var loggerMock = CreateLoggerMock();

            var service = new TextProcessingService(
                chunkMock.Object, aiProviderServiceMock.Object, cacheMock.Object, jobRepositoryMock.Object, loggerMock.Object);


            // Act
            await service.ProcessAsync("long text", jobId);

            // Assert
            // Ожидаем минимум 3 вызова: 2 для чанков + 1 для объединения
            aiProviderServiceMock.Verify(a => a.RequestAsync(It.IsAny<string>()), Times.Exactly(3));
        }

        [Fact]
        public async Task ProcessAsync_OnReduceFailure_ShouldRetryFiveTimes()
        {
            // Arrange
            var jobId = Guid.NewGuid().ToString();
            var chunkMock = new Mock<ITextChunk>();
            chunkMock.Setup(c => c.GetChunks(It.IsAny<string>()))
                .ReturnsAsync(new List<string> { "chunk1", "chunk2" });

            var aiProviderServiceMock = new Mock<IAiProviderService>();
            var sequence = aiProviderServiceMock.SetupSequence(a => a.RequestAsync(It.IsAny<string>()))
                .ReturnsAsync("Chunk 1 Result")
                .ReturnsAsync("Chunk 2 Result");

            // 1 попытка + 5 повторов = 6 исключений
            for(int i = 0; i < 6; i++)
            {
                sequence.ThrowsAsync(new Exception("Reduce API Error"));
            }

            var cacheMock = new Mock<IDistributedCache>();
            var jobRepositoryMock = new Mock<IJobRepository>();
            jobRepositoryMock.Setup(j => j.GetJobByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new JobModel { Id = jobId });

            var fastAnalyzePolicy = Policy<string>
                .Handle<Exception>()
                .WaitAndRetryAsync(3, _ => TimeSpan.Zero);

            var fastReducePolicy = Policy<string>
                .Handle<Exception>()
                .WaitAndRetryAsync(5, _ => TimeSpan.Zero);

            var loggerMock = CreateLoggerMock();

            var service = new TextProcessingService(
                chunkMock.Object, 
                aiProviderServiceMock.Object, 
                cacheMock.Object, 
                jobRepositoryMock.Object,
                loggerMock.Object,
                fastAnalyzePolicy,
                fastReducePolicy);

            // Act
            // Ожидаем, что после всех попыток метод все же выбросит исключение
            await Assert.ThrowsAsync<Exception>(() => service.ProcessAsync("some text", jobId));

            // Assert
            // Проверим: 2 вызова для чанков + 6 попыток для Reduce = 8 вызовов всего
            aiProviderServiceMock.Verify(a => a.RequestAsync(It.IsAny<string>()), Times.Exactly(8));
        }

        [Fact]
        public async Task ProcessAsync_ShouldSetCacheWithExpirationOptions()
        {
            // Arrange
            var jobId = Guid.NewGuid().ToString();
            var chunkMock = new Mock<ITextChunk>();
            chunkMock.Setup(c => c.GetChunks(It.IsAny<string>()))
                .ReturnsAsync(new List<string> { "chunk-to-cache" });

            var aiProviderMock = new Mock<IAiProviderService>();
            aiProviderMock.Setup(a => a.RequestAsync(It.IsAny<string>()))
                .ReturnsAsync("AI Response");

            var cacheMock = new Mock<IDistributedCache>();
            var jobRepositoryMock = new Mock<IJobRepository>();
            jobRepositoryMock.Setup(j => j.GetJobByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new JobModel { Id = jobId });

            var loggerMock = CreateLoggerMock();

            var service = new TextProcessingService(
                chunkMock.Object, aiProviderMock.Object, cacheMock.Object, jobRepositoryMock.Object, loggerMock.Object);

            // Act
            await service.ProcessAsync("some text", jobId);

            // Assert
            // Проверяем, что метод SetAsync был вызван с объектом опций, 
            // в котором установлено время истечения (AbsoluteExpirationRelativeToNow)
            cacheMock.Verify(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.Is<DistributedCacheEntryOptions>(opt => opt.AbsoluteExpirationRelativeToNow != null),
                It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task ProcessAsync_IfSomeChunksFail_shouldRetryEntireBatch()
        {
            // Arrange
            var jobId = "job-batch-cache-test";
            var chunkMock = new Mock<ITextChunk>();
            chunkMock.Setup(c => c.GetChunks(It.IsAny<string>()))
                .ReturnsAsync(new List<string> { "chunk1", "chunk2" });

            var aiProviderMock = new Mock<IAiProviderService>();
            var cacheMock = new Mock<IDistributedCache>();

            // Имитируем работу кэша
            // Сначала в кэше пусто
            cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default)).ReturnsAsync((byte[])null!);

            // Но когда сервис запишет результат первого чанка, мы должны "запомнить" это в мке
            // Для простоты теста: после первого вызова GetAsync (который вернет null),
            // следующий вызов для этого же ключа вернет результат.
            cacheMock.SetupSequence(c => c.GetAsync($"chunk:{jobId}:0", default))
                .ReturnsAsync((byte[])null!) // Первый запуск процесса
                .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes("Result 1")); // Второй (повторный) запуск


            // Настройка AI:
            // Для чанка 1 - всегда успех
            aiProviderMock.Setup(a => a.RequestAsync(It.Is<string>(s => s.Contains("chunk1"))))
                .ReturnsAsync("AI Result 1");

            // Если внутренняя политика делает 3 попытки, нам нужно 3 ошибки,
            // чтобы метод ProcessWithAiAsync в итоге вернул IsSuccess = false
            aiProviderMock.SetupSequence(a => a.RequestAsync(It.Is<string>(s => s.Contains("chunk2"))))
                .ThrowsAsync(new Exception("AI Error 1")) // Попытка 1 внутреннего Polly
                .ThrowsAsync(new Exception("AI Error 2")) // Попытка 2 внутреннего Polly
                .ThrowsAsync(new Exception("AI Error 3")) // Попытка 3 внутреннего Polly
                .ThrowsAsync(new Exception("AI Error 4")) // Попытка 4
                                                          // Теперь внутренняя политика сдается, и метод возвращает IsSuccess = false.
                                                          // Начинается ВТОРАЯ попытка цикла батча:
                .ReturnsAsync("AI Result 2");

            // Для Reduce
            aiProviderMock.Setup(a => a.RequestAsync(It.Is<string>(s => s.Contains("Result"))))
                .ReturnsAsync("Final Summary");
            var jobRepositoryMock = new Mock<IJobRepository>();
            jobRepositoryMock.Setup(j => j.GetJobByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new JobModel { Id = jobId });

            var fastAnalyzePolicy = Policy<string>
                .Handle<Exception>()
                .WaitAndRetryAsync(3, _ => TimeSpan.Zero);

            var fastReducePolicy = Policy<string>
                .Handle<Exception>()
                .WaitAndRetryAsync(5, _ => TimeSpan.Zero);

            var loggerMock = CreateLoggerMock();

            var service = new TextProcessingService(
                chunkMock.Object, 
                aiProviderMock.Object, 
                cacheMock.Object, 
                jobRepositoryMock.Object,
                loggerMock.Object,
                fastAnalyzePolicy,
                fastReducePolicy);

            // Act
            var result = await service.ProcessAsync("text", jobId);

            // Assert
            // 1. Проверяем, что для "chunk1" запрос к AI был выполнен толь ОДИН раз (в первый раз),
            // а во второй раз данные взяты из кэша
            aiProviderMock.Verify(a => a.RequestAsync(It.Is<string>(s => s.Contains("chunk1"))), Times.Once);

            // 2. Проверяем, что "chunk2" запрашивался дважды (первый раз упал, второй раз ок)
            aiProviderMock.Verify(a => a.RequestAsync(It.Is<string>(s => s.Contains("chunk2"))), Times.Exactly(5));

            // 3. Проверяем финальный результат 
            Assert.Equal("Final Summary", result);
        }

        [Fact]
        public async Task ProcessAsync_ShouldRemoveAllChunksFromCache_WhenFinished()
        {
            // Arrange
            string jobId = "cleanup-id";
            var chunks = new List<string> { "c1", "c2" };

            var chunkMock = new Mock<ITextChunk>();
            chunkMock.Setup(c => c.GetChunks(It.IsAny<string>()))
                .ReturnsAsync(chunks);

            var aiProviderMock = new Mock<IAiProviderService>();
            aiProviderMock.Setup(a => a.RequestAsync(It.IsAny<string>()))
                .ReturnsAsync("AI Result");

            var cacheMock = new Mock<IDistributedCache>();
            var jobRepositoryMock = new Mock<IJobRepository>();
            jobRepositoryMock.Setup(j => j.GetJobByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new JobModel { Id = jobId });

            var loggerMock = CreateLoggerMock();

            var service = new TextProcessingService(
                chunkMock.Object, aiProviderMock.Object, cacheMock.Object, jobRepositoryMock.Object, loggerMock.Object);

            // Act
            await service.ProcessAsync("text", jobId);

            // Assert
            // Ожидаем, что для каждого чанка был вызов удаления из кэша
            cacheMock.Verify(c => c.RemoveAsync($"chunk:{jobId}:0", default), Times.Once);
            cacheMock.Verify(c => c.RemoveAsync($"chunk:{jobId}:1", default), Times.Once);
        }

        [Fact]
        public async Task ProcessAsync_CacheFailureDoesNotBreakProcessing()
        {
            // Arrange
            var jobId = "cache-failure-test";
            string text = "Test text for processing";

            var chunkMock = new Mock<ITextChunk>();
            chunkMock.Setup(c => c.GetChunks(It.IsAny<string>()))
                .ReturnsAsync(new List<string> { "chunk1" });

            var aiMock = new Mock<IAiProviderService>();
            aiMock.Setup(a => a.RequestAsync(It.IsAny<string>()))
                .ReturnsAsync("AI Analysis Result");

            // ⚠️ КЛЮЧЕВОЙ МОМЕНТ: Кэш выбрасывает исключение при попытке очистки
            var cacheMock = new Mock<IDistributedCache>();
            cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), default))
                .ThrowsAsync(new Exception("Redis connection failed"));

            // Но SetAsync работает нормально
            cacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(), default))
                .Returns(Task.CompletedTask);

            // И GetAsync тоже работает
            cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
                .ReturnsAsync((byte[])null!);

            var jobRepositoryMock = new Mock<IJobRepository>();
            jobRepositoryMock.Setup(j => j.GetJobByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new JobModel { Id = jobId });

            var loggerMock = CreateLoggerMock();

            var service = new TextProcessingService(
                chunkMock.Object,
                aiMock.Object,
                cacheMock.Object,
                jobRepositoryMock.Object,
                loggerMock.Object);

            // Act
            // ProcessAsync НЕ должен выбросить исключение, несмотря на ошибку кэша
            var result = await service.ProcessAsync(text, jobId);

            // Assert
            // 1. Результат должен быть успешным
            Assert.NotNull(result);
            Assert.Equal("AI Analysis Result", result);

            // 2. Задача должна быть помечена как завершённая
            jobRepositoryMock.Verify(j => j.UpdateAsync(It.Is<JobModel>(job =>
                job.Status == "Completed" &&
                job.SuccessfulChunksCount == 1)), Times.AtLeastOnce);

            // 3. Попытки очистки кэша всё равно были
            cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), default), Times.Once);

            // 4. AI был вызван ровно один раз
            aiMock.Verify(a => a.RequestAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ProcessAsync_OneChunkFails_ShouldSetPartialSuccessStatus()
        {
            // Arrange
            var jobId = "partial-success-job";
            var chunks = new List<string> { "good-chunk", "bad-chunk" };

            var chunkMock = new Mock<ITextChunk>();
            chunkMock.Setup(c => c.GetChunks(It.IsAny<string>())).ReturnsAsync(chunks);

            var aiMock = new Mock<IAiProviderService>();
            // Первый чанк - успех
            aiMock.Setup(a => a.RequestAsync(It.Is<string>(s => s.Contains("good-chunk"))))
                .ReturnsAsync("Good Result");

            // Второй чанк - всегда ошибка
            aiMock.Setup(a => a.RequestAsync(It.Is<string>(s => s.Contains("bad-chunk"))))
                .ThrowsAsync(new Exception("AI Temporary Error"));

            var cacheMock = new Mock<IDistributedCache>();
            var jobRepositoryMock = new Mock<IJobRepository>();
            jobRepositoryMock.Setup(j => j.GetJobByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new JobModel { Id = jobId });

            var fastAnalyzePolicy = Policy<string>
                .Handle<Exception>()
                .WaitAndRetryAsync(3, _ => TimeSpan.Zero);

            var fastReducePolicy = Policy<string>
                .Handle<Exception>()
                .WaitAndRetryAsync(5, _ => TimeSpan.Zero);

            var loggerMock = CreateLoggerMock();

            var service = new TextProcessingService(
                chunkMock.Object,
                aiMock.Object,
                cacheMock.Object,
                jobRepositoryMock.Object,
                loggerMock.Object,
                fastAnalyzePolicy,
                fastReducePolicy);

            // Act
            var result = await service.ProcessAsync("test text", jobId);

            // Assert
            Assert.Contains("Good Result", result);
            jobRepositoryMock.Verify(j => j.UpdateAsync(It.Is<JobModel>(job =>
                job.Status == "PartialSuccess" &&
                job.SuccessfulChunksCount == 1 &&
                job.FailedChunksCount == 1 &&
                job.Error == "Some chunks failed to process.")), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ProcessAsync_AllChunksFail_ShouldThrowAndSetFailedStatus()
        {
            // Arrange
            var jobId = "fail-123";
            var chunks = new List<string> { "chunk1" };

            var chunkMock = new Mock<ITextChunk>();
            chunkMock.Setup(c => c.GetChunks(It.IsAny<string>())).ReturnsAsync(chunks);

            var aiMock = new Mock<IAiProviderService>();
            aiMock.Setup(a => a.RequestAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Critical AI Failure"));

            var cacheMock = new Mock<IDistributedCache>();
            var jobRepositoryMock = new Mock<IJobRepository>();
            jobRepositoryMock.Setup(j => j.GetJobByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new JobModel { Id = jobId });

            var fastAnalyzePolicy = Policy<string>
                .Handle<Exception>()
                .WaitAndRetryAsync(3, _ => TimeSpan.Zero);

            var fastReducePolicy = Policy<string>
                .Handle<Exception>()
                .WaitAndRetryAsync(5, _ => TimeSpan.Zero);

            var loggerMock = CreateLoggerMock();

            var service = new TextProcessingService(
                chunkMock.Object,
                aiMock.Object,
                cacheMock.Object,
                jobRepositoryMock.Object,
                loggerMock.Object,
                fastAnalyzePolicy,
                fastReducePolicy);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                service.ProcessAsync("test text", jobId));

            Assert.Equal("All text chunks failed to process after multiple attempts.", ex.Message);

            // Проверяем, что в конце статус стал Failed
            jobRepositoryMock.Verify(j => j.UpdateAsync(It.Is<JobModel>(job =>
                job.Status == "Failed" &&
                job.SuccessfulChunksCount == 0 &&
                job.FailedChunksCount == 1 &&
                job.Error == "All text chunks failed to process after multiple attempts.")), 
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task ProcessAsync_RecoverAfterRetry_ShouldSetCompletedStatus()
        {
            // Arrange
            var jobId = "retry-ok";
            var chunkMock = new Mock<ITextChunk>();
            chunkMock.Setup(c => c.GetChunks(It.IsAny<string>()))
                .ReturnsAsync(new List<string> { "chunk1" });

            var aiMock = new Mock<IAiProviderService>();
            aiMock.SetupSequence(a => a.RequestAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("First fail"))
                .ReturnsAsync("Success on second attempt");

            var cacheMock = new Mock<IDistributedCache>();
            var jobRepositoryMock = new Mock<IJobRepository>();
            jobRepositoryMock.Setup(j => j.GetJobByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new JobModel { Id = jobId });

            var loggerMock = CreateLoggerMock();

            var service = new TextProcessingService(
                chunkMock.Object,
                aiMock.Object,
                cacheMock.Object,
                jobRepositoryMock.Object, loggerMock.Object);

            // Act
            await service.ProcessAsync("test text", jobId);

            // Assert
            jobRepositoryMock.Verify(j => j.UpdateAsync(It.Is<JobModel>(job =>
                job.Status == "Completed" &&
                job.SuccessfulChunksCount == 1 &&
                job.FailedChunksCount == 0)), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ProcessAsync_PreCancelledToken_ShouldSetCancelledStatus()
        {
            // Arrange
            var jobId = "cancel-1";
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Отменяем токен сразу.

            var chunkMock = new Mock<ITextChunk>();
            chunkMock.Setup(c => c.GetChunks(It.IsAny<string>())).ReturnsAsync(new List<string> { "c1", "c2" });
            var aiMock = new Mock<IAiProviderService>();
            var cacheMock = new Mock<IDistributedCache>();
            var jobRepositoryMock = new Mock<IJobRepository>();
            jobRepositoryMock.Setup(j => j.GetJobByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new JobModel { Id = jobId });

            var loggerMock = CreateLoggerMock();

            var service = new TextProcessingService(
                chunkMock.Object,
                aiMock.Object,
                cacheMock.Object,
                jobRepositoryMock.Object, 
                loggerMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                service.ProcessAsync("some text", jobId, cts.Token));

            // Проверяем, что статус в БД именно Cancelled
            jobRepositoryMock.Verify(j => j.UpdateAsync(It.Is<JobModel>(job =>
                job.Status == "Cancelled" &&
                (job.Error != null && job.Error.Contains("cancelled by user or system")))), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ProcessAsync_CancelledDuringRetry_ShouldStopAndSetCancelledStatus()
        {
            // Arrange
            var jobId = "cancel-mid-run";
            var cts = new CancellationTokenSource();
            var chunks = new List<string> { "chunk1" };

            var chunkMock = new Mock<ITextChunk>();
            chunkMock.Setup(c => c.GetChunks(It.IsAny<string>())).ReturnsAsync(chunks);
            var aiMock = new Mock<IAiProviderService>();
            aiMock.Setup(a => a.RequestAsync(It.IsAny<string>()))
                .Callback(() => cts.Cancel())
                .ThrowsAsync(new Exception("Temporary failure"));

            var cacheMock = new Mock<IDistributedCache>();
            var jobRepositoryMock = new Mock<IJobRepository>();
            jobRepositoryMock.Setup(j => j.GetJobByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new JobModel { Id = jobId });

            var loggerMock = CreateLoggerMock();

            var service = new TextProcessingService(
                chunkMock.Object,
                aiMock.Object,
                cacheMock.Object,
                jobRepositoryMock.Object, 
                loggerMock.Object);

            // Act && Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.ProcessAsync("test text", jobId, cts.Token));

            // Проверяем, что из-за отмены мы не пошли на 2-ю и 3-ю попытку (attempt)
            aiMock.Verify(a => a.RequestAsync(It.IsAny<string>()), Times.Exactly(1));

            jobRepositoryMock.Verify(j => j.UpdateAsync(It.Is<JobModel>(job =>
                job.Status == "Cancelled")), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ProcessAsync_NoChunksGenerated_ShouldThrowAndSetFailedStatus()
        {
            // Arragne
            var jobId = "empty-chunks-123";

            // Мокаем ситуацию, когда сервис вернул пустой список
            var chunkMock = new Mock<ITextChunk>();
            chunkMock.Setup(c => c.GetChunks(It.IsAny<string>())).ReturnsAsync(new List<string>());

            var aiMock = new Mock<IAiProviderService>();
            var cacheMock = new Mock<IDistributedCache>();
            var jobRepositoryMock = new Mock<IJobRepository>();
            jobRepositoryMock.Setup(j => j.GetJobByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new JobModel { Id = jobId });

            var loggerMock = CreateLoggerMock();

            var service = new TextProcessingService(
                chunkMock.Object,
                aiMock.Object,
                cacheMock.Object,
                jobRepositoryMock.Object,
                loggerMock.Object);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                service.ProcessAsync("test text", jobId));

            // Проверяем текст ошибки
            Assert.Equal("No text chunks were generated. " +
                        "The input text might be too short or invalid.", ex.Message);

            // Проверяем, что в базу записался статус Failed
            jobRepositoryMock.Verify(j => j.UpdateAsync(It.Is<JobModel>(job =>
                job.Status == "Failed" &&
                (job.Error != null && job.Error.Contains("No text chunks were")))), Times.AtLeastOnce);

            // Проверяем, что к AI мы даже не пытались обращаться
            aiMock.Verify(a => a.RequestAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ProcessAsync_JobNotFound_ShouldThrowAndSetFailedStatus()
        {
            // Arrange
            var jobId = "non-existent-job";
            var text = "some text";
            var chunkMock = new Mock<ITextChunk>();
            var aiProviderServiceMock = new Mock<IAiProviderService>();
            var cacheMock = new Mock<IDistributedCache>();
            var jobRepositoryMock = new Mock<IJobRepository>();

            // Возвращаем null - job не существует
            jobRepositoryMock.Setup(j => j.GetJobByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((JobModel?)null);

            var loggerMock = CreateLoggerMock();

            var service = new TextProcessingService(
                chunkMock.Object,
                aiProviderServiceMock.Object,
                cacheMock.Object,
                jobRepositoryMock.Object, 
                loggerMock.Object);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                service.ProcessAsync(text, jobId));

            Assert.Equal($"Job with ID {jobId} not found in database. Cannot start processing.", ex.Message);

            // Проверяем, что UpdateAsync НЕ был вызван (так как job == null на самом начале)
            jobRepositoryMock.Verify(j => j.UpdateAsync(It.IsAny<JobModel>()), Times.Never);
        }

        [Fact]
        public async Task ProcessAsync_ShouldSetSymbolsCount()
        {
            // Arrange
            var jobId = "symbols-test";
            string text = "Hello World"; // 11 символов

            var chunkMock = new Mock<ITextChunk>();
            chunkMock.Setup(c => c.GetChunks(It.IsAny<string>()))
                .ReturnsAsync(new List<string> { "Hello", "World" });

            var aiMock = new Mock<IAiProviderService>();
            aiMock.Setup(a => a.RequestAsync(It.IsAny<string>()))
                .ReturnsAsync("Result");

            var cacheMock = new Mock<IDistributedCache>();
            var jobRepositoryMock = new Mock<IJobRepository>();
            jobRepositoryMock.Setup(j => j.GetJobByIdAsync(jobId))
                .ReturnsAsync(new JobModel { Id = jobId });

            var loggerMock = CreateLoggerMock();

            var service = new TextProcessingService(
                chunkMock.Object, aiMock.Object, cacheMock.Object, jobRepositoryMock.Object, loggerMock.Object);

            // Act
            await service.ProcessAsync(text, jobId);

            // Assert
            jobRepositoryMock.Verify(j => j.UpdateAsync(It.Is<JobModel>(job =>
                job.SymbolsCount == 11)), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ProcessAsync_ShouldSetProcessingStatusAtStart()
        {
            // Arrange
            var jobId = "processing-status-test";
            var chunkMock = new Mock<ITextChunk>();
            chunkMock.Setup(c => c.GetChunks(It.IsAny<string>()))
                .ReturnsAsync(new List<string> { "chunk" });

            var aiMock = new Mock<IAiProviderService>();
            aiMock.Setup(a => a.RequestAsync(It.IsAny<string>()))
                .ReturnsAsync("AI Result");

            var cacheMock = new Mock<IDistributedCache>();
            var jobRepositoryMock = new Mock<IJobRepository>();

            // КЛЮЧЕВОЙ МОМЕНТ: Создаём новый объект для GetJobByIdAsync каждый раз
            jobRepositoryMock.Setup(j => j.GetJobByIdAsync(jobId))
                .ReturnsAsync(() => new JobModel { Id = jobId, Status = "Pending" });

            // Используем Callback для захвата аргументов в момент вызова
            var updateAsyncCalls = new List<JobModel>();
            jobRepositoryMock
                .Setup(j => j.UpdateAsync(It.IsAny<JobModel>()))
                .Callback<JobModel>(job =>
                {
                    // Сохраняем КОПИЮ состояния объекта в момент вызова
                    updateAsyncCalls.Add(new JobModel
                    {
                        Id = job.Id,
                        Status = job.Status,
                        SymbolsCount = job.SymbolsCount
                    });
                })
                .ReturnsAsync(It.IsAny<JobModel>());

            var loggerMock = CreateLoggerMock();

            var service = new TextProcessingService(
                chunkMock.Object, aiMock.Object, cacheMock.Object, jobRepositoryMock.Object, loggerMock.Object);

            // Act
            await service.ProcessAsync("text", jobId);

            // Assert
            Assert.Equal(2, updateAsyncCalls.Count);

            // Первый вызов - "Processing"
            Assert.Equal("Processing", updateAsyncCalls[0].Status);
            Assert.Equal(4, updateAsyncCalls[0].SymbolsCount);

            // Второй вызов - "Completed"
            Assert.Equal("Completed", updateAsyncCalls[1].Status);
        }
    }
}
