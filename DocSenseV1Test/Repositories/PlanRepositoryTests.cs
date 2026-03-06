using DocSenseV1.Repositories.Plan;
using DocSenseV1Test.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocSenseV1Test.Repositories
{
    public class PlanRepositoryTests
    {
        // 1. Тест на успешное нахождение плана по типу
        [Fact]
        public async Task GetPlanByTypeAsync_ExistingsType_ReturnsPlanModel()
        {
            // Arrange
            using var context = TestingDbContextFactory.CreateContext();
            IPlanRepository repository = new PlanRepository(context);
            string existingPlanType = "pro";

            // Act
            var result = await repository.GetPlanByTypeAsync(existingPlanType);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(existingPlanType, result.Type);
            Assert.Equal(50000, result.LimitRequests);
        }

        // 2. Тест на ситуацию, когда план не найден
        [Fact]
        public async Task GetPlanByTypeAsync_NonExistingType_ReturnsNull()
        {
            // Arrange
            using var context = TestingDbContextFactory.CreateContext();
            IPlanRepository repository = new PlanRepository(context);
            string existingPlanType = "demon";

            // Act
            var result = await repository.GetPlanByTypeAsync(existingPlanType);

            // Assert
            Assert.Null(result);
        }

        // 3. Тест на проверку, что данные не отслеживаются
        [Fact]
        public async Task GetPlanByTypeAsyn_ReturnsDetachedEntity()
        {
            // Arrange
            using var context = TestingDbContextFactory.CreateContext();
            IPlanRepository repository = new PlanRepository(context);
            string existingPlanType = "basic";

            // Act
            var result = await repository.GetPlanByTypeAsync(existingPlanType);

            // Assert
            Assert.NotNull(result);
            var entry = context.Entry(result);
            Assert.Equal(EntityState.Detached, entry.State);

        }
    }
}
