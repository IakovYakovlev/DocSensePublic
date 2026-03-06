
using DocSenseV1.Models;

namespace DocSenseV1.Repositories.Usage
{
    public interface IUsageRepository
    {
        Task<UsageModel?> CreateAsync(UsageModel usage);
        Task<UsageModel?> UpdateAsync(UsageModel usage);
        Task<UsageModel?> GetUsageByUserIdAndPlanTypeAsync(string user, string planType);
    }
}
