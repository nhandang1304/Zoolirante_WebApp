namespace Zoolirante_Open_Minded.Models
{
    public class PendingEmail
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TicketId { get; set; }
        public DateTime ScheduledTime { get; set; }
        public bool Sent { get; set; } = false;
    }
}
