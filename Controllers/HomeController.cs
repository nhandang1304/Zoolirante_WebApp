using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Zoolirante_Open_Minded.Models;
using Zoolirante_Open_Minded.ViewModels;

namespace Zoolirante_Open_Minded.Controllers
{
	public class HomeController : Controller
	{
		private readonly ZooliranteDatabaseContext _context;

		public HomeController(ZooliranteDatabaseContext context)
		{
			_context = context;
		}

		public IActionResult Index()
		{
			ViewData["BannerText"] = "Welcome to Zoolirante!";

			var today = DateTime.Today;
			var tomorrow = today.AddDays(1);
			var weekLater = today.AddDays(7);

			var eventsToday = _context.Events
				.Where(e => e.StartTime >= today && e.StartTime < tomorrow)
				.Select(e => new EventViewModel
				{
					EventId = e.EventId,
					Title = e.Title,
					Location = e.Location,
					StartTime = e.StartTime,
					EndTime = e.EndTime,
					Description = e.Description,
					ImageUrl = e.ImageUrl
				})
				.ToList();

			var eventsUpcoming = _context.Events
				.Where(e => e.StartTime >= tomorrow && e.StartTime < weekLater)
				.Select(e => new EventViewModel
				{
					EventId = e.EventId,
					Title = e.Title,
					Location = e.Location,
					StartTime = e.StartTime,
					EndTime = e.EndTime,
					Description = e.Description,
					ImageUrl = e.ImageUrl
				})
				.ToList();

			var model = new HomeIndexViewModel
			{
				EventsToday = eventsToday,
				EventsUpcoming = eventsUpcoming
			};

			return View(model);
		}


	}
}
