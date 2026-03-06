using DocSenseV1.Models;

namespace DocSenseV1.Repositories.Job
{
    public interface IJobRepository
    {
        Task<JobModel?> GetJobByIdAsync(string jobId);
        Task<JobModel?> GetJobByUserAndIdAsync(string jobId, string userId);
        Task<JobModel?> CreateAsync(JobModel job);
        Task<JobModel?> UpdateAsync(JobModel job);
    }
}
