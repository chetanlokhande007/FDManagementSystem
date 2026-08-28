using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FinTrustFDManager.Model.Common;

namespace FinTrustFDManager.Model.Entities.MasterData
{
    public class Benchmark : BaseEntity
    {
        [Key]
        public int BenchmarkId { get; set; }

        [Required]
        [MaxLength(100)]
        public string BenchmarkName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public decimal CurrentRate { get; set; }

        [MaxLength(10)]
        public string? RateUnit { get; set; } = "%";

        public ICollection<BenchmarkRateHistory> RateHistory { get; set; } = new List<BenchmarkRateHistory>();
    }
}
