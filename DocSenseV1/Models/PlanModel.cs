using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocSenseV1.Models
{
    [Table("Plans")]
    public class PlanModel
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Type { get; set; } = string.Empty;
        public int LimitRequests { get; set; } = 0;
        public long LimitSymbols { get; set; } = 0;
        public ICollection<UsageModel> Usages { get; set; } = new List<UsageModel>();
    }
}
