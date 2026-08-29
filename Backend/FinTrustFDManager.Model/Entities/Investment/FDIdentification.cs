using System;

namespace FinTrustFDManager.Model.Entities.Investment
{
    public class FDIdentification
    {
        public long FdId { get; set; }

        public string FdReferenceNo { get; set; } = string.Empty;

        public long EntityId { get; set; }

        public long CounterpartyId { get; set; }

        public string CurrencyCode { get; set; } = string.Empty;

        public decimal PrincipalAmount { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public DateTime? SettlementDate { get; set; }

        public long? BankAccountId { get; set; }

        public string Status { get; set; } = "DRAFT";

        public string? Remarks { get; set; }

        public long? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public long? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }
    }
}
