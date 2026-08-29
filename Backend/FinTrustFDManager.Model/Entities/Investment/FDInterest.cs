using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FinTrustFDManager.Model.Entities.CoreData;
using FinTrustFDManager.Model.Entities.MasterData;

namespace FinTrustFDManager.Model.Entities.Investment
{
    public class FDInterest
    {
        public long FdInterestId { get; set; }
        public long FdId { get; set; }

        [Required]
        [MaxLength(10)]
        public string InterestRateType { get; set; } = string.Empty;

        public decimal InterestRate { get; set; }

        // Benchmark (optional - only for FLOATING rate)
        public int? BenchmarkId { get; set; }

        [MaxLength(100)]
        public string? BenchmarkName { get; set; }

        public decimal? BenchmarkRate { get; set; }

        public decimal? Margin { get; set; }

        // Interest Frequency - FK to InterestFrequency master
        public int InterestFrequencyId { get; set; }

        // Compounding Frequency - FK to InterestFrequency master (optional)
        public int? CompoundingFrequencyId { get; set; }

        public bool IsCompounding { get; set; }

        // Day Count Convention - FK to DayCountConvention master
        public int DayCountConventionId { get; set; }

        [MaxLength(50)]
        public string? PaymentConvention { get; set; }

        public DateTime CreatedDate { get; set; }

        // Navigation properties
        [ForeignKey(nameof(InterestFrequencyId))]
        public InterestFrequency? InterestFrequency { get; set; }

        [ForeignKey(nameof(CompoundingFrequencyId))]
        public InterestFrequency? CompoundingFrequencyNavigation { get; set; }

        [ForeignKey(nameof(DayCountConventionId))]
        public DayCountConvention? DayCountConvention { get; set; }

        [ForeignKey(nameof(BenchmarkId))]
        public Benchmark? Benchmark { get; set; }

        // Navigation back to parent FD
        public FDIdentification? FDIdentification { get; set; }
    }
}