using Zoolirante_Open_Minded.Models;

namespace Zoolirante_Open_Minded.ViewModels
{
	public class HomeIndexViewModel
	{
		public List<EventViewModel> EventsToday { get; set; } = new();
		public List<EventViewModel> EventsUpcoming { get; set; } = new();
	}

	public class EventViewModel
	{
		public int EventId { get; set; } = 1;
        public string Title { get; set; }
		public string Location { get; set; }
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
		public string Description { get; set; }
		public string ImageUrl { get; set; }

        public Event Event { get; set; }
        public List<Animal> FeaturingAnimals { get; set; } = new();
        public List<EventViewModel> Related { get; set; } = new();


    }
}
