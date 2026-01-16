using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zoolirante_Open_Minded.Models
{
    public class AnimalFavourite
    {
        
        public int UserId { get; set; }

        [Key]
        public int Id { get; set; }
        public int AnimalId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        [ForeignKey("AnimalId")]
        public Animal Animal { get; set; }
    }
}
