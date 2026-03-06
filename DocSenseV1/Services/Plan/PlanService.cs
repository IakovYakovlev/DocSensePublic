using DocSenseV1.Dtos;

namespace DocSenseV1.Services.Plan
{
    public class PlanService : IPlanService
    {
        private readonly IPlanStrategyFactory _planFactory;

        public PlanService(IPlanStrategyFactory planFactory)
        {
            _planFactory = planFactory;
        }

        public async Task<UploadResponseDto> ExecutePlan(IFormFile file, string user, string planType)
        {
            var strategy = _planFactory.GetStrategy(planType.ToLower());
            return await strategy.ExecuteAsync(file, user);
        }
    }
}
