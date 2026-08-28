using System.ComponentModel.DataAnnotations;

namespace FinTrustFDManager.Model.DTOs.Amendment
{
    public class FDAmendmentActionDto
    {
        [MaxLength(1000)]
        public string? Comments { get; set; }
    }
}
