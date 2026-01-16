using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Zoolirante_Open_Minded.Models;

namespace Zoolirante_Open_Minded.Controllers
{	public class ToggleFavoriteDto
	{
		public int AnimalId { get; set; }
	}

	[Route("api/[controller]")]
	[ApiController]
	public class AnimalFavouriteController : Controller
    {
        private readonly ZooliranteDatabaseContext _context;

        public AnimalFavouriteController(ZooliranteDatabaseContext context)
        {
            _context = context;
        }

		
		public IActionResult Index(string? search)
		{
			var animalF = _context.AnimalFavourite
					  .Include(a => a.Animal)
					  .Include(a => a.User)
					  .AsQueryable();
			if (!string.IsNullOrWhiteSpace(search))
			{
				animalF = animalF.Where(animal => animal.Animal.Name.Contains(search) || animal.User.FullName.Contains(search));
			}

            return View(animalF.ToList());
		}

		[HttpPost("Toggle")]
		public IActionResult Toggle([FromBody] ToggleFavoriteDto fav)
		{
			var userId = HttpContext.Session.GetInt32("UserId");
			if (userId == null)
			{
				return Unauthorized();
			}

			var existing = _context.AnimalFavourite
				.FirstOrDefault(f => f.UserId == userId && f.AnimalId == fav.AnimalId);

			if (existing != null)
			{
				_context.AnimalFavourite.Remove(existing);
				_context.SaveChanges();
				return Ok(new { added = false });
			}
			else
			{
				var newFav = new AnimalFavourite
				{
					UserId = userId.Value,
					AnimalId = fav.AnimalId
				};
				_context.AnimalFavourite.Add(newFav);
				_context.SaveChanges();
				return Ok(new { added = true });
			}
		}

		[HttpGet("Favourite")]
		public IActionResult Favourite()
		{
			var userId = HttpContext.Session.GetInt32("UserId");
			if (userId == null) return Unauthorized();

			var favorites = _context.AnimalFavourite
				.Where(f => f.UserId == userId)
				.Select(f => f.AnimalId)
				.ToList();

			return Ok(favorites);
		}

		[HttpGet("/AnimalFavourite/UserFavorites")]
		public async Task<IActionResult> UserFavorites(String? searchName, String? searchSpecies, String? searchConservation, String? searchRegion)
		{
			ViewData["BannerText"] = "Explore the amazing animals of Zoolirante";

			var userId = HttpContext.Session.GetInt32("UserId");
			if (userId == null)
				return RedirectToAction("Login", "Users");

			var favoriteAnimalIds = await _context.AnimalFavourite
				.Where(f => f.UserId == userId)
				.Select(f => f.AnimalId)
				.ToListAsync();

			var animal = _context.Animals
				.Where(a => favoriteAnimalIds.Contains(a.AnimalId))
				.AsQueryable();

			ViewBag.searchName = searchName;

			// Creating species dropdown list
			var species = animal.Select(x => x.Species).ToList();
			ViewBag.speciesList = new SelectList(species, selectedValue: searchSpecies);

			// Creating conservation dropdown list
			var conservationStatus = animal.Select(x => x.ConservationStatus).Distinct().ToList();
			ViewBag.conservation = new SelectList(conservationStatus, selectedValue: searchConservation);

			// Creating region dropdown list
			var regions = animal.Select(x => x.Region).Distinct().ToList();
			ViewBag.regionList = new SelectList(regions, selectedValue: searchRegion);
			// Filtering
			if (!String.IsNullOrWhiteSpace(searchName))
			{
				animal = animal.Where(x => x.Name.Contains(searchName));
			}
			if (!String.IsNullOrEmpty(searchSpecies))
			{
				animal = animal.Where(x => x.Species == searchSpecies);
			}
			if (!String.IsNullOrEmpty(searchConservation))
			{
				animal = animal.Where(x => x.ConservationStatus == searchConservation);
			}
			if (!string.IsNullOrEmpty(searchRegion))
			{
				animal = animal.Where(x => x.Region == searchRegion);
			}

			return View("UserFavorites", await animal.ToListAsync());
		}
	}
}
