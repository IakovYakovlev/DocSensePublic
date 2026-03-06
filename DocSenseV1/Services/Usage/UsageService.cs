using DocSenseV1.Dtos;
using DocSenseV1.Models;
using DocSenseV1.Repositories.Usage;

namespace DocSenseV1.Services.Usage
{
    public class UsageService : IUsageService
    {
        private readonly IUsageRepository _usageRepo;

        public UsageService(IUsageRepository usageRepo)
        {
            _usageRepo = usageRepo;
        }

        public async Task<UsageCheckResult> CheckLimitsAsync(string user, PlanModel plan, long requestedSymbols)
        {
            var usage = await PrepareUsageForCheckAsync(user, plan);

            bool canUse = usage.TotalSymbols + requestedSymbols <= plan.LimitSymbols
                && usage!.TotalRequests + 1 <= plan.LimitRequests;

            return new UsageCheckResult
            {
                Allowed = canUse,
                Stats = new UsageLimitsStats
                {
                    Symbols = new SymbolsMetric
                    {
                        Used = usage.TotalSymbols,
                        Limit = plan.LimitSymbols,
                        Remaining = plan.LimitSymbols - usage.TotalSymbols,
                        RequestedSymbols = requestedSymbols,
                    },
                    Requests = new UsageLimitMetric
                    {
                        Used = usage.TotalRequests,
                        Limit = plan.LimitRequests,
                        Remaining = plan.LimitRequests - usage.TotalRequests,
                    }
                }
            };
        }

        public async Task<UsageModel?> IncrementUsageAsync(string userId, string planType, int addedSymbols)
        {
            var usage = await _usageRepo.GetUsageByUserIdAndPlanTypeAsync(userId, planType);

            if (usage is null)
            {
                throw new InvalidOperationException($"Cannot increment usage: record for user {userId} and plan {planType} not found.");
            }

            usage.TotalSymbols += addedSymbols;
            usage.TotalRequests += 1;

            return await _usageRepo.UpdateAsync(usage);
        }

        private async Task<UsageModel> PrepareUsageForCheckAsync(string user, PlanModel plan)
        {
            // 1. Достаем юзера из базы.
            var usage = await _usageRepo.GetUsageByUserIdAndPlanTypeAsync(user, plan.Type);

            // 2. Если юзера еще нет, тогда создаем его.
            if (usage is null)
            {
                UsageModel newUsage = new UsageModel
                {
                    UserId = user,
                    PlanId = plan.Id,
                    TotalRequests = 0,
                    TotalSymbols = 0,
                    PeriodStart = DateTime.UtcNow.Date,
                    CreatedAt = DateTime.UtcNow.Date
                };
                usage = await _usageRepo.CreateAsync(newUsage)
                    ?? throw new InvalidOperationException("Failed to create usage record.");
            }

            // 3. Если наступил нужный момент, тогда обнуляем данные.
            if (DateTime.UtcNow.Date >= usage!.PeriodStart.AddMonths(1).Date)
            {
                usage.TotalRequests = 0;
                usage.TotalSymbols = 0;
                usage.PeriodStart = DateTime.UtcNow.Date;
                usage = await _usageRepo.UpdateAsync(usage)
                    ?? throw new InvalidOperationException("Failed to update usage after reset.");
            }

            return usage!;
        }
    }
}
