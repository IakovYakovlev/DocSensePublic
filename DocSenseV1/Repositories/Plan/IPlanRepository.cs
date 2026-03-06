using DocSenseV1.Models;

namespace DocSenseV1.Repositories.Plan
{
    public interface IPlanRepository
    {
        Task<PlanModel?> GetPlanByTypeAsync(string planType);
    }
}
