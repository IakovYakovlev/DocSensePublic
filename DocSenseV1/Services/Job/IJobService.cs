using DocSenseV1.Models;

namespace DocSenseV1.Services.Job
{
    public interface IJobService
    {
        Task CreateJobAsync(string text, string userId, int planId, string jobId);
        Task<JobModel?> GetJobResultAsync(string jobId, string userId);
    }
}
