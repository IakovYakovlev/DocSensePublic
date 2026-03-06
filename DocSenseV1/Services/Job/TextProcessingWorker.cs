
using DocSenseV1.Services.TextProcessing;
using System.Runtime.InteropServices;

namespace DocSenseV1.Services.Job
{
    public class TextProcessingWorker : BackgroundService
    {
        private readonly IJobQueue _queue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TextProcessingWorker> _logger;

        public TextProcessingWorker(
            IJobQueue queue, 
            IServiceProvider serviceProvider, 
            ILogger<TextProcessingWorker> logger)
        {
            _queue = queue;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TextProcessingWorker запущен.");

            // Опционально: ограничиваем количество одновременно работающих задач (например, 5)
            // Если лимит не нужен, можно обойтись без SemaphoreSlim
            using var semaphore = new SemaphoreSlim(10);

            // Бесконечный цикл обработки задач из очереди
            await foreach(var task in _queue.DequeueAllAsync(stoppingToken))
            {
                _logger.LogInformation("Воркер взял задачу {JobId} в работу.", task.JobId);

                _ = Task.Run(async () =>
                {
                    await semaphore.WaitAsync(stoppingToken);
                    try
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var processingService = scope.ServiceProvider.GetRequiredService<ITextProcessingService>();
                            // Вызываем реальную обработку текста
                            await processingService.ProcessAsync(task.Text, task.JobId);
                        }

                        _logger.LogInformation("Воркер успешно завершил задачу {JobId}.", task.JobId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Критическая ошибка при обработке JobId: {JobId}", task.JobId);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, stoppingToken);
            }
        }
    }
}
