using DocSenseV1.Data;
using DocSenseV1.Models;
using Microsoft.EntityFrameworkCore;

namespace DocSenseV1.Repositories.Plan
{
    public class PlanRepository : IPlanRepository
    {
        private readonly EFDatabaseContext _context;

        public PlanRepository(EFDatabaseContext context)
        {
            _context = context;
        }

        public Task<PlanModel?> GetPlanByTypeAsync(string planType) =>
            _context.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Type == planType);
    }
}
