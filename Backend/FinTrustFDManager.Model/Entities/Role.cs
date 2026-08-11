using System.ComponentModel.DataAnnotations;
using FinTrustFDManager.Model.Common;

namespace FinTrustFDManager.Model.Entities
{
    public class Role : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string RoleName { get; set; } = string.Empty;

        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
