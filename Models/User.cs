using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zoolirante_Open_Minded.Models
{
    public partial class User
    {
        public int UserId { get; set; }

        [Required]
        [Display(Name = "Full Name")]
        [StringLength(200, ErrorMessage = "Full Name cannot exceed 200 characters.")]
        public string FullName { get; set; } = null!;

        [Required]
        [Display(Name = "Email")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = null!;

        [Required]
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        public string PasswordHash { get; set; } = null!;

        public string Role { get; set; } = null!;

        // Nullable fields for EF Core
        public string? ResetToken { get; set; }
        public DateTime? TokenExpiry { get; set; }
        public string? PaymentMethod { get; set; }

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
        public virtual UserDetail? UserDetail { get; set; }
        public ICollection<AnimalFavourite> AnimalFavourites { get; set; }
    }
}
