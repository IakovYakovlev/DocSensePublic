using DocSenseV1.Models;
using DocSenseV1.Repositories.Job;
using DocSenseV1.Services.Job;
using Microsoft.Extensions.Caching.Distributed;
using Moq;

namespace DocSenseV1Test.Services.Job
{
    public class JobServiceTest
    {
        [Fact]
        public async Task CreateJobAsync_ShouldSaveToDb_SetCache_AndEnqueueTask()
        {
            // Arrange
            var jobId = Guid.NewGuid().ToString();
            var text = "sample text";
            var userId = "user123";
            var planId = 1;

            var jobRepositoryMock = new Mock<IJobRepository>();
            var queueMock = new Mock<IJobQueue>();

            var service = new JobService(jobRepositoryMock.Object, queueMock.Object);

            // Act
            await service.CreateJobAsync(text, userId, planId, jobId);

            // Assert
            jobRepositoryMock.Verify(j => j.CreateAsync(It.Is<JobModel>(job =>
                job.Status == "Pending")), Times.Once());

            // 2. Проверка очереди
            queueMock.Verify(q => q.EnqueueAsync(
                It.Is<JobTask>(t => t.JobId == jobId && t.Text == text),
                It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task GetJobResultAsync_ShouldReturnJob_WhenJobExists()
        {
            // Arrange
            var jobId = Guid.NewGuid().ToString();
            var userId = "user 123";

            var jobRepositoryMock = new Mock<IJobRepository>();
            jobRepositoryMock.Setup(j => j.GetJobByUserAndIdAsync(jobId, userId))
                .ReturnsAsync(new JobModel
                {
                    Id = jobId,
                    Status = "Completed",
                    Result = "Processed text"
                });

            var queueMock = new Mock<IJobQueue>();

            IJobService service = new JobService(jobRepositoryMock.Object, queueMock.Object);

            // Act
            JobModel? result = await service.GetJobResultAsync(jobId, userId);

            // Assert
            Assert.NotNull(result);
        }
    }
}
