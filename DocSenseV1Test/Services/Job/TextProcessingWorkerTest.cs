using Castle.Core.Logging;
using DocSenseV1.Services.Job;
using DocSenseV1.Services.TextProcessing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocSenseV1Test.Services.Job
{
    public class TextProcessingWorkerTest
    {
        [Fact]
        public async Task Worker_ShouldCallProcessingService_WhenTaskIsEnqueued()
        {
            // Arrange
            var jobId = Guid.NewGuid().ToString();
            var taskData = new JobTask(jobId, "Sample content", "user123", 1);

            // Создаем реальную очередь (Singleton)
            var queue = new JobQueue(capacity: 10);

            // Мокаем сервис обработки текста
            var textProcMock = new Mock<ITextProcessingService>();

            // Воркерку нужен IServiceProvider, чтобы создать Scope (облать видимости)
            // Мы подготовим моки для DI (Dependecy Injection)
            var serviceProviderMock = new Mock<IServiceProvider>();
            var scopeMock = new Mock<IServiceScope>();
            var scopeFactoryMock = new Mock<IServiceScopeFactory>();

            // Настраиваем цепочку: Provider -> ScopeFactory -> Scope -> ServiceProvider -> ITextProcessingService
            serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactoryMock.Object);
            scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
            scopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
            serviceProviderMock.Setup(x => x.GetService(typeof(ITextProcessingService))).Returns(textProcMock.Object);

            var loggerMock = new Mock<ILogger<TextProcessingWorker>>();

            var worker = new TextProcessingWorker(queue, serviceProviderMock.Object, loggerMock.Object);

            // Act
            // Кладем задачу в очередь ДО старта воркера
            await queue.EnqueueAsync(taskData);

            using var cts = new CancellationTokenSource();
            // Запускаем воркер в отдельном Task
            var executeTask = worker.StartAsync(cts.Token);

            // Ждем совсем немного, чтобы воркер успел подхватить задачу
            await Task.Delay(100);

            // Останавливаем воркер
            await worker.StopAsync(cts.Token);

            // 3. Assert (Проверка)
            // Самое важное: проверяем, был ли вызван метод обработки с нашими данными
            textProcMock.Verify(x => x.ProcessAsync(
                "Sample content",
                jobId,
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
