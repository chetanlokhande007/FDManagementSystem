using FinTrustFDManager.Model.Entities.MasterData;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinTrustFDManager.Model.Entities.CoreData
{
    public class Investment
    {
        public int Id { get; set; }

        // Auto-generated reference
        public string InvestmentReferenceNo { get; set; } = string.Empty;

        // Master Data Foreign Keys
        public int EntityId { get; set; }
        public int CountryId { get; set; }
        public int CurrencyId { get; set; }
        public int BankId { get; set; }
        public int BankAccountId { get; set; }

        public int InterestFrequencyId { get; set; }
        public int DayCountConventionId { get; set; }

        // Investment Details
        public decimal PrincipalAmount { get; set; }

        public decimal InterestRate { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string? Remarks { get; set; }

        // Workflow Status
        public string Status { get; set; } = "Draft";

        // Audit
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public string? CreatedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public string? ModifiedBy { get; set; }

        // Navigation Properties
        public Entity Entity { get; set; } = null!;

        public Country Country { get; set; } = null!;

        public Currency Currency { get; set; } = null!;

        public Bank Bank { get; set; } = null!;

        public BankAccount BankAccount { get; set; } = null!;

        public InterestFrequency InterestFrequency { get; set; } = null!;

        public DayCountConvention DayCountConvention { get; set; } = null!;

        public ICollection<InvestmentApproval> Approvals { get; set; }
            = new List<InvestmentApproval>();

        public ICollection<CashFlow> CashFlows { get; set; }
            = new List<CashFlow>();
    }
}
