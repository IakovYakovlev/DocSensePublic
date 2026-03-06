using DocSenseV1.Repositories.Plan;
using DocSenseV1.Services.FileReader;
using DocSenseV1.Services.Job;
using DocSenseV1.Services.Plan;
using DocSenseV1.Services.Plan.Strategies;
using DocSenseV1.Services.Usage;
using Moq;

namespace DocSenseV1Test.Services.Plan
{
    public class PlanStrategyFactoryTest
    {
        [Theory]
        [InlineData("basic")]
        [InlineData("BASIC")]
        [InlineData("pro")]
        [InlineData("PRO")]
        [InlineData("ultra")]
        [InlineData("ULTRA")]
        public void GetPlanStrategy_ShouldReturnCorrectStrategy_WhenSupported(string planType)
        {
            // Arrange
            var fileReaderMock = new Mock<IFileReaderService>();
            var planRepoMock = new Mock<IPlanRepository>();
            var usageMock = new Mock<IUsageService>();
            var jobServiceMock = new Mock<IJobService>();

            var strategies = new List<IPlanStrategy>
            {
                new BasicPlanStrategy(fileReaderMock.Object, planRepoMock.Object, usageMock.Object, jobServiceMock.Object),
                new ProPlanStrategy(fileReaderMock.Object, planRepoMock.Object, usageMock.Object, jobServiceMock.Object),
                new UltraPlanStrategy(fileReaderMock.Object, planRepoMock.Object, usageMock.Object, jobServiceMock.Object),
            };
            var factory = new PlanStrategyFactory(strategies);

            // Act
            var result = factory.GetStrategy(planType);

            // Assert
            Assert.NotNull(result);
        }

        [Theory]
        [InlineData("Ultra1")]
        [InlineData("unsupported_plan")]
        [InlineData("FR EE")]
        [InlineData(" prO")]
        public void GetPlanStrategy_ShouldThrowNotSupportedException_WhenNotSupported(string planType)
        {
            // Arrange
            var fileReaderMock = new Mock<IFileReaderService>();
            var planRepoMock = new Mock<IPlanRepository>();
            var usageMock = new Mock<IUsageService>();
            var jobServiceMock = new Mock<IJobService>();

            var strategies = new List<IPlanStrategy>
            {
                new BasicPlanStrategy(fileReaderMock.Object, planRepoMock.Object, usageMock.Object, jobServiceMock.Object),
                new ProPlanStrategy(fileReaderMock.Object, planRepoMock.Object, usageMock.Object, jobServiceMock.Object),
                new UltraPlanStrategy(fileReaderMock.Object, planRepoMock.Object, usageMock.Object, jobServiceMock.Object),
            };
            var factory = new PlanStrategyFactory(strategies);

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => factory.GetStrategy(planType));
        }
    }
}
