using DocSenseV1.Dtos;
using DocSenseV1.Services.Plan;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocSenseV1Test.Services.Plan
{
    public class PlanServiceTest
    {
        [Fact]
        public async Task ExecutePlan_CallsFactoryAndStrategy_Successfully()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            var user = "testUser";
            var planType = "FullProcess";

            // Ожидаемый результат
            var expectedResponse = new UploadResponseDto { Status = "done" };

            var mockStrategy = new Mock<IPlanStrategy>();
            mockStrategy.Setup(s => s.ExecuteAsync(mockFile.Object, user)).ReturnsAsync(expectedResponse);

            var mockFactory = new Mock<IPlanStrategyFactory>();
            mockFactory.Setup(f => f.GetStrategy(planType.ToLower())).Returns(mockStrategy.Object);

            var service = new PlanService(mockFactory.Object);

            // Act
            var actualResponse = await service.ExecutePlan(mockFile.Object, user, planType);

            // Assert

            // Проверка, что фабрика была вызвана с правильным типом плана
            mockFactory.Verify(s => s.GetStrategy(planType.ToLower()), Times.Once,
                "Фабрика должна быть вызвана для получения стратегии.");

            // Проверка, что метод ExecuteAsync на мок-стратегии был вызван с правильным аргументом
            mockStrategy.Verify(s => s.ExecuteAsync(mockFile.Object, user), Times.Once,
                "Стратегия должна быть вызвана для выполнения плана.");

            // Проверка, что резульатат переда корректно.
            Assert.Equal(expectedResponse, actualResponse);
            Assert.Equal("done", actualResponse.Status);

        }
    }
}
