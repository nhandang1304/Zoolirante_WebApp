using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Zoolirante_Open_Minded.Models;

namespace Zoolirante_Open_Minded.Controllers
{
    public class AnimalsController : Controller
    {
        private readonly ZooliranteDatabaseContext _context;

        public AnimalsController(ZooliranteDatabaseContext context)
        {
            _context = context;
        }

        // GET: Animals
        public async Task<IActionResult> Index(String? searchName, String? searchSpecies, String? searchConservation, String? searchRegion)
        {
            ViewData["BannerText"] = "Explore the amazing animals of Zoolirante";

            var animal = _context.Animals.AsQueryable();

            ViewBag.searchName = searchName;

            // Creating species dropdown list
            var species = animal.Select(x=>x.Species).ToList();          
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
			return View(await animal.ToListAsync());
        }

		// GET: Animals/Details/5
		public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var animal = await _context.Animals
                .FirstOrDefaultAsync(m => m.AnimalId == id);
            if (animal == null)
            {
                return NotFound();
            }

            return View(animal);
        }

        // GET: Animals/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Animals/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AnimalId,Name,Species,Region,ConservationStatus,Habitat,Description,ImageUrl,ExhibitLocation")] Animal animal)
        {
            if (ModelState.IsValid)
            {
                _context.Add(animal);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(animal);
        }

        // GET: Animals/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var animal = await _context.Animals.FindAsync(id);
            if (animal == null)
            {
                return NotFound();
            }
            return View(animal);
        }

        // POST: Animals/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AnimalId,Name,Species,Region,ConservationStatus,Habitat,Description,ImageUrl,ExhibitLocation")] Animal animal)
        {
            if (id != animal.AnimalId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(animal);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AnimalExists(animal.AnimalId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(animal);
        }

        // GET: Animals/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var animal = await _context.Animals
                .FirstOrDefaultAsync(m => m.AnimalId == id);
            if (animal == null)
            {
                return NotFound();
            }

            return View(animal);
        }

        // POST: Animals/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var animal = await _context.Animals.FindAsync(id);
            if (animal != null)
            {
                _context.Animals.Remove(animal);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AnimalExists(int id)
        {
            return _context.Animals.Any(e => e.AnimalId == id);
        }
    }
}
