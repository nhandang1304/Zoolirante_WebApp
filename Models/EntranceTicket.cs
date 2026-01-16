using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Zoolirante_Open_Minded.Models
{
    public class EntranceTicket
    {
        [Key]
        public int TicketId { get; set; }

        
        public int UserId { get; set; }

        
        public string Type { get; set; } 
        
        public decimal Price { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime VisitDate { get; set; } 
        public DateTime ExpiredAt  => VisitDate.AddHours(12);

        public string Details { get; set; }
        public User User { get; set; }
    }
}
