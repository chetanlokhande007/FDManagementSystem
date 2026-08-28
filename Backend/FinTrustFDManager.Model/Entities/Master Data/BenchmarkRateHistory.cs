using System;
using System.ComponentModel.DataAnnotations;
using FinTrustFDManager.Model.Common;

namespace FinTrustFDManager.Model.Entities.MasterData
{
    public class BenchmarkRateHistory : BaseEntity
    {
        [Key]
        public long BenchmarkRateHistoryId { get; set; }

        public int BenchmarkId { get; set; }
        public Benchmark? Benchmark { get; set; }

        public decimal Rate { get; set; }

        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }
    }
}
