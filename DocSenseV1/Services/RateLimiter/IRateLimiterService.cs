namespace DocSenseV1.Services.RateLimiter
{
    public interface IRateLimiterService
    {
        Task<bool> IsAllowedAsync(string user, string plan);
    }
}
