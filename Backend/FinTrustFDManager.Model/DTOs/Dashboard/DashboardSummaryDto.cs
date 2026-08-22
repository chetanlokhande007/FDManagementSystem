using System;
using System.Collections.Generic;

namespace FinTrustFDManager.Model.DTOs.Dashboard
{
    public class DashboardSummaryDto
    {
        public int ActiveFDCount { get; set; }
        public decimal TotalPrincipal { get; set; }
        public decimal TotalAccruedInterest { get; set; }
        public int MaturingThisMonthCount { get; set; }
        public decimal MaturingThisMonthValue { get; set; }
        
        public List<ChartDataDto> FDGrowthData { get; set; } = new();
        public List<ChartDataDto> PortfolioDistributionData { get; set; } = new();
        
        public List<FDUpcomingMaturityDto> UpcomingMaturities { get; set; } = new();
        public List<FDRecentDto> RecentlyAddedFDs { get; set; } = new();
    }

    public class ChartDataDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public int Count { get; set; }
    }

    public class FDUpcomingMaturityDto
    {
        public long FdId { get; set; }
        public string FdReferenceNo { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public decimal PrincipalAmount { get; set; }
        public DateTime MaturityDate { get; set; }
        public decimal MaturityAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class FDRecentDto
    {
        public long FdId { get; set; }
        public string FdReferenceNo { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public decimal PrincipalAmount { get; set; }
        public decimal InterestRate { get; set; }
        public string InterestType { get; set; } = string.Empty;
    }
}
