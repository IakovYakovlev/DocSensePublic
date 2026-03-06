namespace DocSenseV1.Services.Plan
{
    public class PlanStrategyFactory : IPlanStrategyFactory
    {
        private readonly IEnumerable<IPlanStrategy> _strategies;

        public PlanStrategyFactory(IEnumerable<IPlanStrategy> strategies)
        {
            _strategies = strategies;
        }

        public IPlanStrategy GetStrategy(string planType)
        {
            var strategy = _strategies.FirstOrDefault(s => s.CanHandle(planType));
            if(strategy == null)
                throw new NotSupportedException($"No strategy found for plan type: {planType}");

            return strategy;
        }
    }
}
