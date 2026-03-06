namespace DocSenseV1.Services.RateLimiter
{
    public class RateLimiterService : IRateLimiterService
    {
        private readonly Dictionary<string, (int count, DateTime resetTime)> _requestCounts = new();
        private readonly object _lock = new();

        public Task<bool> IsAllowedAsync(string user, string plan)
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;

                var requestLimit = plan.ToLower() switch
                {
                    "basic" => 10,
                    "pro" => 100,
                    "ultra" => 1000,
                    _ => 10 // Default to Free plan limits
                };

                if (_requestCounts.TryGetValue(user, out var entry))
                {
                    // Если минута прошла - сбрасываем счётчик
                    if (now > entry.resetTime)
                    {
                        _requestCounts[user] = (1, now.AddMinutes(1));
                        return Task.FromResult(true);
                    }

                    // Если лимит исчерпан
                    if (entry.count >= requestLimit)
                    {
                        return Task.FromResult(false);
                    }

                    // Увеличиваем счётчик
                    _requestCounts[user] = (entry.count + 1, entry.resetTime);
                    return Task.FromResult(true);
                }

                // Первый запрос
                _requestCounts[user] = (1, now.AddMinutes(1));
                return Task.FromResult(true);
            }
        }
    }
}
