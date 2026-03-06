namespace DocSenseV1.Dtos
{
    public class UsageCheckResult
    {
        public bool Allowed { get; set; } = false;
        public UsageLimitsStats Stats { get; set; } = new UsageLimitsStats();
    }
}
