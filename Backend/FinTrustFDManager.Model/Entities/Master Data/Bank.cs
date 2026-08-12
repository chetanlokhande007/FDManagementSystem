using FinTrustFDManager.Model.Common;
using System.ComponentModel.DataAnnotations;

namespace FinTrustFDManager.Model.Entities.MasterData;

public class Bank : BaseEntity
{
    [Key]
    public int BankId { get; set; }

    [Required]
    [MaxLength(20)]
    public string BankCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string BankName { get; set; } = string.Empty;

    public int CountryId { get; set; }
    public Country? Country { get; set; }

    // One Bank -> Many FDs
    //public ICollection<FD> FDs { get; set; } = new List<FD>();
}