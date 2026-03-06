using DocSenseV1.Dtos;
using DocSenseV1.Models;
using DocSenseV1.Repositories.Plan;
using DocSenseV1.Repositories.Usage;
using DocSenseV1.Services.Usage;
using Moq;

namespace DocSenseV1Test.Services.Usage
{
    public class UsageServiceTest
    {
        private const string TestUser = "user_123";
        private readonly PlanModel TestPlan = new PlanModel { Id = 1, Type = "basic", LimitRequests = 100, LimitSymbols = 6000 };
        private const int TestSymbols = 5000;

        [Fact]
        public async Task UsageService_FirstUse_CreatesRecordAndAllowsUse()
        {
            // Arrange
            var usageRepo = new Mock<IUsageRepository>();
            usageRepo.Setup(r => r.GetUsageByUserIdAndPlanTypeAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((UsageModel?)null);
            usageRepo.Setup(r => r.CreateAsync(It.IsAny<UsageModel>()))
                .Returns<UsageModel>(model =>
                {
                    model.Id = 999;
                    return Task.FromResult((UsageModel?)model);
                });
            IUsageService usageService = new UsageService(usageRepo.Object);
            int requestedSymbols = 6001;

            // Act
            var result = await usageService.CheckLimitsAsync(TestUser, TestPlan, requestedSymbols);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Allowed); // Ожидаем, что лимит превышен

            // Проверяем, что метод CreateAsync был вызван ровно 1 раз
            usageRepo.Verify(r => r.CreateAsync(It.IsAny<UsageModel>()), Times.Once());
        }

        [Fact]
        public async Task UsageService_ExistingUser_AllowsUse()
        {
            // Arrange
            var usageRepo = new Mock<IUsageRepository>();
            var existingUsage = new UsageModel
            {
                Id = 1,
                UserId = TestUser,
                PlanId = 1,
                TotalRequests = 5,
                TotalSymbols = 50,
                PeriodStart = DateTime.UtcNow.Date
            };
            usageRepo.Setup(r => r.GetUsageByUserIdAndPlanTypeAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingUsage);
            IUsageService usageService = new UsageService(usageRepo.Object);

            // Act
            var result = await usageService.CheckLimitsAsync(TestUser, TestPlan, TestSymbols);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<UsageCheckResult>(result);
            Assert.True(result.Allowed);

            // Проверяем, что метод CreateAsync не был вызван
            usageRepo.Verify(r => r.CreateAsync(It.IsAny<UsageModel>()), Times.Never());
        }

        [Fact]
        public async Task UsageService_ExistingUser_BadSymbolsRequestCount_AllowedFalse()
        {
            // Arrange
            var usageRepo = new Mock<IUsageRepository>();
            usageRepo.Setup(r => r.GetUsageByUserIdAndPlanTypeAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((UsageModel?)new UsageModel
                {
                    Id = 1,
                    UserId = "user_123",
                    PlanId = 1,
                    TotalSymbols = 0,
                    TotalRequests = 30,
                });
            IUsageService usageService = new UsageService(usageRepo.Object);
            int requestedSymbols = 6001;

            // Act
            var result = await usageService.CheckLimitsAsync(TestUser, TestPlan, requestedSymbols);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<UsageCheckResult>(result);
            Assert.False(result.Allowed);

            // Проверям, что метод CreateAsync не вызывался
            usageRepo.Verify(r => r.CreateAsync(It.IsAny<UsageModel>()), Times.Never());
        }

        [Fact]
        public async Task UsageService_ExistingUser_BadSymbolsRequestCountByUser_AllowedFalse()
        {
            // Arrange
            var usageRepo = new Mock<IUsageRepository>();
            usageRepo.Setup(r => r.GetUsageByUserIdAndPlanTypeAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new UsageModel
                {
                    Id = 1,
                    UserId = "user_123",
                    PlanId = 1,
                    TotalSymbols = 4000,
                    TotalRequests = 30,
                });
            // Предположим, что юзер уже использовал ранее. Теперь у него осталось только 2000 свободных символов
            // а он запрашивает 3000
            IUsageService usageService = new UsageService(usageRepo.Object);
            int requestedSymbols = 3000;

            // Act
            var result = await usageService.CheckLimitsAsync(TestUser, TestPlan, requestedSymbols);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<UsageCheckResult>(result);
            Assert.False(result.Allowed);

            // Проверим, что метод CreateAsync не вызывался
            usageRepo.Verify(r => r.CreateAsync(It.IsAny<UsageModel>()), Times.Never());
        }

        [Fact]
        public async Task UsageService_PeriodRefresh_AfterOneMonth_ResetsLimitsAndUpdatesDatabase()
        {
            // Arrange
            // Дата, когда пользователь в первый раз начал использовать (месяц назад)
            var periodStartDate = DateTime.UtcNow.AddMonths(-1).Date;

            var usageRepo = new Mock<IUsageRepository>();
            var existingUsage = new UsageModel
            {
                Id = 1,
                UserId = TestUser,
                PlanId = 1,
                TotalSymbols = 5500,      // Пользователь уже потратил 5500 символов
                TotalRequests = 95,       // И 95 запросов
                PeriodStart = periodStartDate,
                CreatedAt = periodStartDate
            };

            usageRepo.Setup(r => r.GetUsageByUserIdAndPlanTypeAsync(TestUser, TestPlan.Type))
                .ReturnsAsync(existingUsage);

            usageRepo.Setup(r => r.UpdateAsync(It.IsAny<UsageModel>()))
                .Returns<UsageModel>(model => Task.FromResult((UsageModel?)model));

            IUsageService usageService = new UsageService(usageRepo.Object);
            int requestedSymbols = 1000;

            // Act
            // Пытаемся использовать ещё 1000 символов (сейчас это месяц спустя)
            var result = await usageService.CheckLimitsAsync(TestUser, TestPlan, requestedSymbols);

            // Assert
            // После сброса лимитов, у нас есть 1000 новых символов + 100 запросов
            Assert.NotNull(result);
            Assert.True(result.Allowed);

            // Проверяем статистику - она должна быть после сброса
            Assert.Equal(0, result.Stats.Symbols.Used);           // Было 5500, стало 0
            Assert.Equal(TestPlan.LimitSymbols, result.Stats.Symbols.Limit);
            Assert.Equal(TestPlan.LimitSymbols, result.Stats.Symbols.Remaining);
            Assert.Equal(requestedSymbols, result.Stats.Symbols.RequestedSymbols);

            Assert.Equal(0, result.Stats.Requests.Used);          // Было 95, стало 0
            Assert.Equal(TestPlan.LimitRequests, result.Stats.Requests.Limit);
            Assert.Equal(TestPlan.LimitRequests, result.Stats.Requests.Remaining);

            // Проверяем, что UpdateAsync был вызван для сброса данных
            usageRepo.Verify(r => r.UpdateAsync(It.Is<UsageModel>(u =>
                u.TotalSymbols == 0 &&
                u.TotalRequests == 0 &&
                u.PeriodStart == DateTime.UtcNow.Date &&
                u.Id == existingUsage.Id
            )), Times.Once(), "UpdateAsync должен быть вызван с обнулёнными значениями");
        }

        [Fact]
        public async Task UsageService_PeriodNotExpired_DoesNotReset()
        {
            // Arrange
            // Дата - день назад (период ещё не истёк)
            var periodStartDate = DateTime.UtcNow.AddDays(-1).Date;

            var usageRepo = new Mock<IUsageRepository>();
            var existingUsage = new UsageModel
            {
                Id = 2,
                UserId = "user_456",
                PlanId = 1,
                TotalSymbols = 3000,
                TotalRequests = 50,
                PeriodStart = periodStartDate,
                CreatedAt = periodStartDate
            };

            usageRepo.Setup(r => r.GetUsageByUserIdAndPlanTypeAsync("user_456", TestPlan.Type))
                .ReturnsAsync(existingUsage);

            IUsageService usageService = new UsageService(usageRepo.Object);
            int requestedSymbols = 500;

            // Act
            var result = await usageService.CheckLimitsAsync("user_456", TestPlan, requestedSymbols);

            // Assert
            // Статистика должна быть старой (не сброшена)
            Assert.NotNull(result);
            Assert.True(result.Allowed);
            Assert.Equal(3000, result.Stats.Symbols.Used);        // Не изменилось
            Assert.Equal(50, result.Stats.Requests.Used);         // Не изменилось

            // UpdateAsync НЕ должен быть вызван
            usageRepo.Verify(r => r.UpdateAsync(It.IsAny<UsageModel>()), Times.Never(),
                "UpdateAsync не должен вызваться если период не истёк");
        }

        [Fact]
        public async Task UsageService_CycleExpired_ResetsUsageAndAllowsUse()
        {
            // Arrange
            var usageRepo = new Mock<IUsageRepository>();
            usageRepo.Setup(r => r.GetUsageByUserIdAndPlanTypeAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new UsageModel
                {
                    Id = 1,
                    UserId = "user_123",
                    PlanId = 1,
                    TotalSymbols = 5500,
                    TotalRequests = 100,
                    PeriodStart = new DateTime(2025, 11, 01)
                });
            usageRepo.Setup(r => r.UpdateAsync(It.IsAny<UsageModel>()))
                .Returns<UsageModel>(model =>
                {
                    return Task.FromResult((UsageModel?)model);
                });
            IUsageService usageService = new UsageService(usageRepo.Object);

            int requesteSymbols = 5000;

            // Act
            var result = await usageService.CheckLimitsAsync(TestUser, TestPlan, requesteSymbols);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Allowed);

            // Проверяем, что сброс произошел.
            usageRepo.Verify(r => r.UpdateAsync(It.IsAny<UsageModel>()), Times.Once());
            usageRepo.Verify(r => r.UpdateAsync(It.Is<UsageModel>(u =>
            u.TotalSymbols == 0 &&
            u.TotalRequests == 0 &&
            u.PeriodStart == DateTime.UtcNow.Date)), Times.Once());
        }

        [Fact]
        public async Task UsageService_ReturnsCorrectStats()
        {
            // Arrange
            const long usedSymbols = 1500;
            const int usedRequests = 10;
            const long requestedSymbols = 500;

            var usageRepo = new Mock<IUsageRepository>();
            usageRepo.Setup(r => r.GetUsageByUserIdAndPlanTypeAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new UsageModel
                {
                    Id = 1,
                    UserId = TestUser,
                    PlanId = 1,
                    TotalSymbols = usedSymbols,
                    TotalRequests = usedRequests,
                    PeriodStart = DateTime.UtcNow.Date,
                });

            IUsageService usageService = new UsageService(usageRepo.Object);

            // Act
            var result = await usageService.CheckLimitsAsync(TestUser, TestPlan, requestedSymbols);


            // Assert
            Assert.NotNull(result);
            Assert.True(result.Allowed);

            var stats = result.Stats;

            Assert.Equal(usedSymbols, stats.Symbols.Used);
            Assert.Equal(TestPlan.LimitSymbols, stats.Symbols.Limit);
            Assert.Equal(TestPlan.LimitSymbols - usedSymbols, stats.Symbols.Remaining);
            Assert.Equal(requestedSymbols, stats.Symbols.RequestedSymbols);

            // Запросы
            Assert.Equal(usedRequests, stats.Requests.Used);
            Assert.Equal(TestPlan.LimitRequests, stats.Requests.Limit);
            Assert.Equal(TestPlan.LimitRequests - usedRequests, stats.Requests.Remaining);
        }

        [Fact]
        public async Task IncrementUsageAsync_ShouldUpdateExistingRecord()
        {
            // Arrange
            var userId = "user-1";
            var planType = "basic";
            var initialSymbols = 100;
            var initialRequests = 2;
            var addedSymbols = 500;

            // Объект который уже якобы уже есть в базе
            var existingUsage = new UsageModel
            {
                Id = 1,
                UserId = userId,
                PlanId = 1,
                TotalSymbols = initialSymbols,
                TotalRequests = initialRequests,
                PeriodStart = DateTime.UtcNow.Date
            };

            var usageRepoMock = new Mock<IUsageRepository>();

            // Мокаем получение по пользователю и типу плана
            usageRepoMock.Setup(r => r.GetUsageByUserIdAndPlanTypeAsync(userId, planType))
                .ReturnsAsync(existingUsage);

            var service = new UsageService(usageRepoMock.Object);

            // Act
            await service.IncrementUsageAsync(userId, planType, addedSymbols);

            // Assert
            // Проверяем математику: 100 + 500 = 600
            Assert.Equal(600, existingUsage.TotalSymbols);

            // Проверяем инкремент запроса: 2 + 1 = 3
            Assert.Equal(3, existingUsage.TotalRequests);
        }
    }
}
