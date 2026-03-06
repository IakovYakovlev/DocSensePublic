using DocSenseV1.Dtos;
using DocSenseV1.Models;
using DocSenseV1.Repositories.Plan;
using DocSenseV1.Services.FileReader;
using DocSenseV1.Services.Job;
using DocSenseV1.Services.Plan.Strategies;
using DocSenseV1.Services.Usage;
using Microsoft.AspNetCore.Http;
using Moq;

namespace DocSenseV1Test.Services.Plan
{
    public class PlanStrategiesTest
    {
        private const string TestUser = "user_123";

        [Theory]
        [InlineData("Краткий тестовый текст.", 23)]
        [InlineData("Очень длинный текст для проверки символов, включая пробелы и пунктуацию.", 72)]
        public async Task ExecuteFreeStrategy_ShouldReaqdContentAndPassCorrectSymbolsCountToProcessText(
            string mockContent,
            int expectedSymbols)
        {
            // Arrange
            var mockReader = new Mock<IFileReaderService>();
            mockReader.Setup(r => r.ReadFileAsync(It.IsAny<IFormFile>())).ReturnsAsync(mockContent);
            var mockPlanRepo = new Mock<IPlanRepository>();
            mockPlanRepo.Setup(p => p.GetPlanByTypeAsync(It.IsAny<string>()))
                .ReturnsAsync(new PlanModel
                {
                    Id = 1,
                    Type = "basic",
                });
            var mockUsageServ = new Mock<IUsageService>();
            mockUsageServ.Setup(u => u.CheckLimitsAsync(It.IsAny<string>(), It.IsAny<PlanModel>(), It.IsAny<long>()))
                .ReturnsAsync(new UsageCheckResult
                {
                    Allowed = true,
                });


            var mockFile = new Mock<IFormFile>();
            var jobServiceMock = new Mock<IJobService>();

            var strategy = new BasicPlanStrategy(
                mockReader.Object,
                mockPlanRepo.Object,
                mockUsageServ.Object,
                jobServiceMock.Object);

            // Act
            var result = await strategy.ExecuteAsync(mockFile.Object, TestUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("basic", result.Plan);
            Assert.Equal("pending", result.Status);

            Assert.Equal(expectedSymbols, result.Stats.Symbols.RequestedSymbols);

            mockPlanRepo.Verify(p => p.GetPlanByTypeAsync(It.IsAny<string>()), Times.Once());
            mockReader.Verify(r => r.ReadFileAsync(mockFile.Object), Times.Once,
                "BasePlanStrategy.ExecuteAsync() не вызвал IFileReaderService.ReadAsync()");
            jobServiceMock.Verify(j => j.CreateJobAsync(
                It.IsAny<string>(),
                TestUser,
                It.IsAny<int>(),
                It.IsAny<string>()),
                Times.Once
            );
        }

        [Fact]
        public async Task ExecuteAsync_ShouldIncrementUsage_WhenJobIsEnqueued()
        {
            // Arrange
            var userId = "user-123";
            var content = "Hello world"; // 11 символов
            var symbols = content.Length;

            var fileMock = new Mock<IFormFile>();
            var fileReaderMock = new Mock<IFileReaderService>();
            fileReaderMock.Setup(x => x.ReadFileAsync(It.IsAny<IFormFile>())).ReturnsAsync(content);

            var planRepoMock = new Mock<IPlanRepository>();
            var planModel = new PlanModel { Id = 1, Type = "Free" };
            planRepoMock.Setup(x => x.GetPlanByTypeAsync(It.IsAny<string>())).ReturnsAsync(planModel);

            var usageServiceMock = new Mock<IUsageService>();
            // Имитируем, что лимиты позволяют обработку
            usageServiceMock.Setup(x => x.CheckLimitsAsync(userId, planModel, symbols))
                .ReturnsAsync(new UsageCheckResult { Allowed = true });

            var jobServiceMock = new Mock<IJobService>();

            // Создаем экземпляр тестируемой стратегии (например, FreePlanStrategy)
            var strategy = new BasicPlanStrategy(
                fileReaderMock.Object,
                planRepoMock.Object,
                usageServiceMock.Object,
                jobServiceMock.Object);

            // Act
            await strategy.ExecuteAsync(fileMock.Object, userId);

            // Assert
            // Самое важное: проверяем, что IncrementUsageAsync был вызван 1 раз 
            // именно для этого пользователя и с этим количеством символов
            usageServiceMock.Verify(x => x.IncrementUsageAsync(userId, planModel.Type, symbols), Times.Once());
        }
    }
}
