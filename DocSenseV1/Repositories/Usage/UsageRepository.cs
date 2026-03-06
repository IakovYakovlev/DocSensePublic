using DocSenseV1.Data;
using DocSenseV1.Models;
using Microsoft.EntityFrameworkCore;

namespace DocSenseV1.Repositories.Usage
{
    public class UsageRepository : IUsageRepository
    {
        private readonly EFDatabaseContext _context;

        public UsageRepository(EFDatabaseContext context)
        {
            _context = context;
        }

        public async Task<UsageModel?> GetUsageByUserIdAndPlanTypeAsync(string user, string planType)
        {
            return await _context.Usages
                .Include(u => u.Plan)
                .FirstOrDefaultAsync(u => u.UserId == user && u.Plan != null && u.Plan.Type == planType);
        }

        public async Task<UsageModel?> CreateAsync(UsageModel usage)
        {
            usage.Id = 0; // Ensure the ID is set to 0 for auto-increment

            _context.Usages.Add(usage);
            await _context.SaveChangesAsync();

            return usage;
        }

        public async Task<UsageModel?> UpdateAsync(UsageModel usage)
        {
            _context.Usages.Update(usage);

            try
            {
                await _context.SaveChangesAsync();
                return usage;
            }
            catch (DbUpdateConcurrencyException)
            {
                return null;
            }
        }
    }
}
