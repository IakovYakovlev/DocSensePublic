using DocSenseV1.Data;
using DocSenseV1.Models;
using Microsoft.EntityFrameworkCore;

namespace DocSenseV1.Repositories.Job
{
    public class JobRepository : IJobRepository
    {
        private readonly EFDatabaseContext _context;

        public JobRepository(EFDatabaseContext context)
        {
            _context = context;
        }

        public async Task<JobModel?> GetJobByIdAsync(string jobId)
            => await _context.Jobs.FindAsync(jobId);

        public async Task<JobModel?> GetJobByUserAndIdAsync(string jobId, string userId)
            => await _context.Jobs
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == jobId && j.UserId == userId);

        public async Task<JobModel?> CreateAsync(JobModel job)
        {
            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();

            return job;
        }

        public async Task<JobModel?> UpdateAsync(JobModel job)
        {
            job.UpdatedAt = DateTime.UtcNow;

            _context.Jobs.Update(job);
            await _context.SaveChangesAsync();

            return job;
        }
    }
}
