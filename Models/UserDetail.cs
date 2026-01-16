using System.ComponentModel.DataAnnotations;

namespace Zoolirante_Open_Minded.Models
{
    public class UserDetail
    {
        public int UserDetailId { get; set; }
        public int UserId { get; set; }

        [Required, StringLength(100)]
        public string FirstName { get; set; } = "";

        [StringLength(100)]
        public string? MiddleName { get; set; }

        [Required, StringLength(100)]
        public string LastName { get; set; } = "";

        [StringLength(30)]
        public string? Phone { get; set; }

        [StringLength(150)]
        public string? Street { get; set; }

        public User User { get; set; } = null!;
    }
}

