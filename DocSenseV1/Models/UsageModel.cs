using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocSenseV1.Models
{
    [Table("Usage")]
    public class UsageModel
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; } = String.Empty;
        public int PlanId { get; set; }
        public PlanModel? Plan { get; set; } = null;
        public long TotalSymbols { get; set; } = 0;
        public int TotalRequests { get; set; } = 0;
        public DateTime PeriodStart { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
