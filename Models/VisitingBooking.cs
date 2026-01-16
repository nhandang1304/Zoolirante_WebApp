using System;

namespace Zoolirante_Open_Minded.Models
{
    public class VisitBooking
    {
        public int VisitBookingId { get; set; }
        public int UserId { get; set; }
        public int TimeSlotId { get; set; }
        public string Status { get; set; } = "Booked"; 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; }
        public TimeSlot TimeSlot { get; set; }
    }
}