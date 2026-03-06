using DocSenseV1.Models;
using DocSenseV1.Repositories.Usage;
using DocSenseV1Test.Data;
using Microsoft.EntityFrameworkCore;

namespace DocSenseV1Test.Repositories
{
    public class UsageRepositoryTest
    {
        // 1. Тест на успешное нахождение usage по userId и planType
        [Fact]
        public async Task GetUsageByUserIdAndPlanType_ExistingUserAndPlan_ReturnsUsageModel()
        {
            // Arrange
            using var context = TestingDbContextFactory.CreateContext();
            IUsageRepository repository = new UsageRepository(context);
            string existingUserId = "user_1";
            string existingPlanType = "basic";

            // Act
            var result = await repository.GetUsageByUserIdAndPlanTypeAsync(existingUserId, existingPlanType);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Plan);
            Assert.Equal(existingUserId, result.UserId);
            Assert.Equal(existingPlanType, result.Plan.Type);
        }

        // 2. Тест на ситуацию, когда usage не найден
        [Fact]
        public async Task GetUsageByUserIdAndPlanTypeAsync_NonExistingUsage_ReturnNull()
        {
            // Arrange
            using var context = TestingDbContextFactory.CreateContext();
            IUsageRepository repository = new UsageRepository(context);
            string nonExistingUserId = "user_999";
            string nonExistingPlanType = "premium";

            // Act
            var result = await repository.GetUsageByUserIdAndPlanTypeAsync(nonExistingUserId, nonExistingPlanType);

            // Assert
            Assert.Null(result);
        }

        // 3. Тест на проверку, что данные не отслеживаются
        [Fact]
        public async Task GetUsageByUserIdAndPlanTypeAsync_ReturnDetachedEntity()
        {
            // Arrange
            using var context = TestingDbContextFactory.CreateContext();
            IUsageRepository repository = new UsageRepository(context);
            string existingUserId = "user_1";
            string existingPlanType = "pro";

            // Act
            var result = await repository.GetUsageByUserIdAndPlanTypeAsync(existingUserId, existingPlanType);

            // Assert
            Assert.NotNull(result);
            var entry = context.Entry(result);
            Assert.Equal(EntityState.Unchanged, entry.State);
        }

        // 4. Тест на создание нового UsageModel
        [Fact]
        public async Task CreateAsync_ValidUsageModel_ReturnsCreatedUsageModel()
        {
            // Arrange
            using var context = TestingDbContextFactory.CreateContext();
            IUsageRepository repository = new UsageRepository(context);
            var newUsage = new DocSenseV1.Models.UsageModel
            {
                UserId = "user_10",
                PlanId = 1,
                TotalSymbols = 5000,
                TotalRequests = 5,
                PeriodStart = DateTime.UtcNow.Date,
                CreatedAt = DateTime.UtcNow.Date
            };

            // Act
            var result = await repository.CreateAsync(newUsage);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(0, result.Id);
            Assert.Equal(newUsage.UserId, result.UserId);
            Assert.Equal(newUsage.PlanId, result.PlanId);
            Assert.Equal(newUsage.TotalSymbols, result.TotalSymbols);
        }

        // 5. Тест на обновление UsageModel
        [Fact]
        public async Task UpdateAsync_ValidUsageModel_ReturnsUpdatedUsageModel()
        {
            // Arrange
            var sharedDbName = Guid.NewGuid().ToString();
            using (var context1 = TestingDbContextFactory.CreateContext(sharedDbName))
            {
                var repository1 = new UsageRepository(context1);
                var newUsageModel = new UsageModel
                {
                    UserId = "user_update",
                    PlanId = 2,
                    TotalSymbols = 1000,
                    TotalRequests = 10,
                    PeriodStart = DateTime.UtcNow.Date,
                    CreatedAt = DateTime.UtcNow.Date
                };

                var createdUsage = await repository1.CreateAsync(newUsageModel);
                Assert.NotNull(createdUsage);

                // Act
                using (var context2 = TestingDbContextFactory.CreateContext(sharedDbName))
                {
                    var repository2 = new UsageRepository(context2);

                    UsageModel updateUsageModel = new UsageModel
                    {
                        Id = createdUsage.Id,
                        UserId = "user_update",
                        PlanId = 2,
                        TotalSymbols = 2000,
                        TotalRequests = 20,
                        PeriodStart = DateTime.UtcNow.Date,
                        CreatedAt = DateTime.UtcNow.Date
                    };

                    var updatedUsage = await repository2.UpdateAsync(updateUsageModel);

                    // Assert
                    Assert.NotNull(updatedUsage);
                    Assert.Equal(updateUsageModel.TotalRequests, updatedUsage.TotalRequests);
                    Assert.Equal(updateUsageModel.TotalSymbols, updatedUsage.TotalSymbols);
                }
            }

        }
    }
}
