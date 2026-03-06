using DocSenseV1.Dtos;
using DocSenseV1.Models;
using DocSenseV1.Repositories.Plan;
using DocSenseV1.Services.FileReader;
using DocSenseV1.Services.Job;
using DocSenseV1.Services.TextProcessing;
using DocSenseV1.Services.Usage;

namespace DocSenseV1.Services.Plan.Strategies
{
    public class ProPlanStrategy : BasePlanStrategy
    {
        protected override string PlanType => "pro";

        public ProPlanStrategy(IFileReaderService reader, IPlanRepository planRepo,
            IUsageService usage, IJobService jobService) 
                : base(reader, planRepo, usage, jobService) { }
    }
}
