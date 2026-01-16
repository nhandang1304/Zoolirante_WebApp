using System;
using System.Collections.Generic;

namespace Zoolirante_Open_Minded.Models
{
    public class TimeSlot
    {
        public int TimeSlotId { get; set; }
        public DateTime Date { get; set; }          
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public int Capacity { get; set; } = 50;
        public int BookedCount { get; set; } = 0;
        public bool IsClosed { get; set; } = false;

        public ICollection<VisitBooking> Bookings { get; set; }
    }
}