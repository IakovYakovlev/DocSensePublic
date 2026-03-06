using DocSenseV1.Dtos;

namespace DocSenseV1.Exceptions
{
    public class UsageLimitException : Exception
    {
        public UsageLimitsStats Stats { get; set; }

        public UsageLimitException(string message, UsageLimitsStats stats): base(message) 
        {
            Stats = stats;
        }
    }
}
