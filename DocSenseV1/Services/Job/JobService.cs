
using DocSenseV1.Models;
using DocSenseV1.Repositories.Job;

namespace DocSenseV1.Services.Job
{
    public class JobService : IJobService
    {
        private readonly IJobRepository _jobRepo;
        private readonly IJobQueue _queue;

        public JobService(IJobRepository jobRepo, IJobQueue queue)
        {
            _jobRepo = jobRepo;
            _queue = queue;
        }

        public async Task CreateJobAsync(string text, string userId, int planId, string jobId)
        {
            // 1. Сохраняем в базу данных
            var job = new JobModel
            {
                Id = jobId,
                UserId = userId,
                PlanId = planId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _jobRepo.CreateAsync(job);

            // 2. Отправляем в очередь
            var task = new JobTask(jobId, text, userId, planId);
            await _queue.EnqueueAsync(task);
        }

        public async Task<JobModel?> GetJobResultAsync(string jobId, string userId)
        {
            return await _jobRepo.GetJobByUserAndIdAsync(jobId, userId);
        }
    }
}
