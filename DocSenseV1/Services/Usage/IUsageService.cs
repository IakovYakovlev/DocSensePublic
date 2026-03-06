using DocSenseV1.Dtos;
using DocSenseV1.Models;

namespace DocSenseV1.Services.Usage
{
    public interface IUsageService
    {
        Task<UsageCheckResult> CheckLimitsAsync(string user, PlanModel planType, long requestedSymbols);
        Task<UsageModel?> IncrementUsageAsync(string userId, string planType, int addedSymbols);
    }
}
