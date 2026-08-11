using System.ComponentModel.DataAnnotations;
using FinTrustFDManager.Model.Common;

namespace FinTrustFDManager.Model.Entities
{
    public class User : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(15)]
        public string MobileNo { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;
    }
}
