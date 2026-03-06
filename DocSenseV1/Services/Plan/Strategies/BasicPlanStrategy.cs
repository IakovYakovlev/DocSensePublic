using DocSenseV1.Repositories.Plan;
using DocSenseV1.Services.FileReader;
using DocSenseV1.Services.Job;
using DocSenseV1.Services.Usage;

namespace DocSenseV1.Services.Plan.Strategies
{
    public class BasicPlanStrategy : BasePlanStrategy
    {
        protected override string PlanType => "basic";

        public BasicPlanStrategy(IFileReaderService reader, IPlanRepository planRepo,
            IUsageService usage, IJobService jobService)
                : base(reader, planRepo, usage, jobService) { }

    }
}
