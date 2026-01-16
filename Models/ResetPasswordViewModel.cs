using System.ComponentModel.DataAnnotations;

namespace Zoolirante_Open_Minded.Models
{
    public class ResetPasswordViewModel
    {
        public string Email { get; set; }
        public string Token { get; set; }

        [Required, DataType(DataType.Password)]
        public string NewPassword { get; set; }
    }
}
