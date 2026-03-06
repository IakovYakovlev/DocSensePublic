using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocSenseV1.Models
{
    [Table("Job", Schema = "public")]
    public class JobModel
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = string.Empty; // text в SQL

        [Column("userId")]
        public string UserId { get; set; } = string.Empty;

        [Column("planId")]
        public int PlanId { get; set; }

        [Column("status")]
        public string Status { get; set; } = string.Empty;

        [Column("result", TypeName = "jsonb")]
        public string? Result { get; set; } // jsonb в SQL

        [Column("error")]
        public string? Error { get; set; }

        [Column("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("totalChunks")]
        public int TotalChunks { get; set; }

        [Column("successfulChunksCount")]
        public int SuccessfulChunksCount { get; set; }

        [Column("failedChunksCount")]
        public int FailedChunksCount { get; set; }

        [Column("executionTimeMs")]
        public double ExecutionTimeMs { get; set; }

        [Column("symbolsCount")]
        public int SymbolsCount { get; set; }

        [Column("resultSymbolsCount")]
        public int ResultSymbolsCount { get; set; }
    }
}
