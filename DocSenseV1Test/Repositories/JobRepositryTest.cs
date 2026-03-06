using DocSenseV1.Models;
using DocSenseV1.Repositories.Job;
using DocSenseV1Test.Data;

namespace DocSenseV1Test.Repositories
{
    public class JobRepositryTest
    {
        [Fact]
        public async Task GetJobByIdAsync_ShouldReturnJob_WhenJobExists()
        {
            // Arrange
            using var context = TestingDbContextFactory.CreateContext();
            IJobRepository repository = new JobRepository(context);
            var jobId = Guid.NewGuid().ToString();
            var job = new JobModel
            {
                Id = jobId,
                UserId = "test-user",
                PlanId = 1,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            context.Jobs.Add(job);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetJobByIdAsync(jobId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(jobId, result!.Id);
            Assert.Equal("test-user", result.UserId);
        }

        [Fact]
        public async Task GetJobByUserAndIdAsync_ShouldReturnJob_WhenJobExists()
        {
            // Arrange
            using var context = TestingDbContextFactory.CreateContext();
            IJobRepository repository = new JobRepository(context);
            var jobId = Guid.NewGuid().ToString();
            var userId = "test-user";
            var job = new JobModel
            {
                Id = jobId,
                UserId = userId,
                PlanId = 1,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            context.Jobs.Add(job);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetJobByUserAndIdAsync(jobId, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(jobId, result!.Id);
            Assert.Equal(userId, result.UserId);
        }

        [Fact]
        public async Task GetJobByUserAndIdAsync_ShouldReturnNull_WhenUserNotExists()
        {
            // Arrange
            using var context = TestingDbContextFactory.CreateContext();
            IJobRepository repository = new JobRepository(context);
            var jobId = Guid.NewGuid().ToString();
            var userId = "test-user";
            var job = new JobModel
            {
                Id = jobId,
                UserId = userId,
                PlanId = 1,
                Status = "Complete",
                CreatedAt = DateTime.UtcNow
            };
            context.Jobs.Add(job);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetJobByUserAndIdAsync(jobId, "Fake user");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetJobByUserAndIdAsync_ShouldReturnNull_WhenJobIdNotExists()
        {
            // Arrange
            using var context = TestingDbContextFactory.CreateContext();
            IJobRepository repository = new JobRepository(context);
            var jobId = Guid.NewGuid().ToString();
            var userId = "test-user";
            var job = new JobModel
            {
                Id = jobId,
                UserId = userId,
                PlanId = 1,
                Status = "Complete",
                CreatedAt = DateTime.UtcNow
            };
            context.Jobs.Add(job);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetJobByUserAndIdAsync("Fake Job Id", userId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Createasync_ShouldSaveJobToDatabase()
        {
            // Arrange
            using var context = TestingDbContextFactory.CreateContext();
            IJobRepository repository = new JobRepository(context);
            var jobId = Guid.NewGuid().ToString();
            JobModel job = new JobModel
            {
                Id = jobId,
                UserId = "test-user",
                PlanId = 1,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var result = await repository.CreateAsync(job);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(jobId, result!.Id);

            // Проверим физическое наличие в базе данных
            var jobInDb = await context.Jobs.FindAsync(jobId);
            Assert.NotNull(jobInDb);
            Assert.Equal(1, jobInDb.PlanId);
            Assert.Equal("Pending", jobInDb.Status);
        }

        [Fact]
        public async Task UpdateResultAsync_ShouldUpdateStatusAndJsonResult()
        {
            // Arrange
            using var context = TestingDbContextFactory.CreateContext();
            IJobRepository repository = new JobRepository(context);

            var jobId = Guid.NewGuid().ToString();
            var job = new JobModel
            {
                Id = jobId,
                UserId = "test-user",
                PlanId = 1,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            context.Jobs.Add(job);
            await context.SaveChangesAsync();

            var expectedStatus = "Success";
            var expectedResult = "{\"summary\": \"Текст успешно обработан\"}";

            job.Status = expectedStatus;
            job.Result = expectedResult;


            // Act
            await repository.UpdateAsync(job);

            // Assert
            var jobInDb = await context.Jobs.FindAsync(jobId);
            Assert.Equal(expectedStatus, jobInDb!.Status);
            Assert.Equal(expectedResult, jobInDb.Result);
        }
    }
}
