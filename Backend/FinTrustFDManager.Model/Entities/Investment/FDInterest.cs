using System;

namespace FinTrustFDManager.Model.Entities.Investment
{
    public class FDInterest
    {
        public long FdInterestId { get; set; }
        public long FdId { get; set; }

        public string InterestRateType { get; set; } = string.Empty;
        public decimal InterestRate { get; set; }

        public string? BenchmarkName { get; set; }
        public decimal? BenchmarkRate { get; set; }
        public decimal? Margin { get; set; }

        public string InterestFrequency { get; set; } = string.Empty;
        public string? CompoundingFrequency { get; set; }
        public bool IsCompounding { get; set; }
        public string CalculationBasis { get; set; } = string.Empty;

        public string? PaymentConvention { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}