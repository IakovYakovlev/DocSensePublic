namespace DocSenseV1.Dtos
{
    public class UsageLimitsStats
    {
        public SymbolsMetric Symbols { get; set; } = new SymbolsMetric();
        public UsageLimitMetric Requests { get; set; } = new UsageLimitMetric();
    }
}
