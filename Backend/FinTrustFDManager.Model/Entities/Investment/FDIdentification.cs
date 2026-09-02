using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FinTrustFDManager.Model.Entities.MasterData;

namespace FinTrustFDManager.Model.Entities.Investment
{
    public class FDIdentification
    {
        public long FdId { get; set; }

        [Required]
        [MaxLength(20)]
        public string FdReferenceNo { get; set; } = string.Empty;

        // Entity FK (Our organization / investing legal entity)
        public int EntityId { get; set; }

        // CounterParty FK (External institution)
        public int CounterpartyId { get; set; }

        // Currency FK
        public int CurrencyId { get; set; }

        public decimal PrincipalAmount { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public DateTime? SettlementDate { get; set; }



        [MaxLength(20)]
        public string Status { get; set; } = "DRAFT";

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public long? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public long? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        // Navigation properties
        [ForeignKey(nameof(EntityId))]
        public Entity? Entity { get; set; }

        [ForeignKey(nameof(CounterpartyId))]
        public CounterParty? CounterParty { get; set; }

        [ForeignKey(nameof(CurrencyId))]
        public Currency? CurrencyNavigation { get; set; }


    }
}
