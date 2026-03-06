namespace DocSenseV1.Services.Plan
{
    public interface IPlanStrategyFactory
    {
        IPlanStrategy GetStrategy(string planType);
    }
}
