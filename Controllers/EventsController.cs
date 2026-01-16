using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zoolirante_Open_Minded.Models;
using Zoolirante_Open_Minded.ViewModels;

namespace Zoolirante_Open_Minded.Controllers
{
    public class EventsController : Controller
    {
        private readonly ZooliranteDatabaseContext _context;

        public EventsController(ZooliranteDatabaseContext context)
        {
            _context = context;
        }

        // GET: Events
        public IActionResult Index(string? q, string? sort = "soonest")
        {
			ViewData["BannerText"] = "Explore our events";
			var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var weekLater = today.AddDays(7);

            IQueryable<Event> baseQuery = _context.Events;

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                baseQuery = baseQuery.Where(e =>
                    e.Title.Contains(term) ||
                    (e.Description != null && e.Description.Contains(term)) ||
                    e.Location.Contains(term));
            }

            baseQuery = sort switch
            {
                "latest" => baseQuery.OrderByDescending(e => e.StartTime),
                _ => baseQuery.OrderBy(e => e.StartTime)
            };

            var eventsToday = baseQuery
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

            var eventsUpcoming = baseQuery
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

            return View(new HomeIndexViewModel
            {
                EventsToday = eventsToday,
                EventsUpcoming = eventsUpcoming
            });
        }

        // /Events/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return NotFound();

            // Uses skip-navigation Include (needs the join table to exist)
            var ev = await _context.Events
                .Include(e => e.Animals)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (ev is null) return NotFound();

            // 3 related upcoming events (not this one)
            var related = await _context.Events
                .Where(e => e.EventId != ev.EventId && e.StartTime >= DateTime.Today)
                .OrderBy(e => e.StartTime).Take(3)
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
                .ToListAsync();

            var vm = new EventViewModel
            {
                Event = ev,
                FeaturingAnimals = ev.Animals.ToList(),
                Related = related
            };

            return View(vm);
        }

        // /Events/Ics/5
        [HttpGet]
        public async Task<IActionResult> Ics(int id)
        {
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventId == id);
            if (ev is null) return NotFound();

            string Esc(string s) => s?.Replace(",", "\\,").Replace(";", "\\;").Replace("\n", "\\n") ?? "";
            string dtStart = ev.StartTime.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'");
            string dtEnd = ev.EndTime.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'");

            var ics = new StringBuilder()
                .AppendLine("BEGIN:VCALENDAR")
                .AppendLine("VERSION:2.0")
                .AppendLine("PRODID:-//Zoolirante//Events//EN")
                .AppendLine("CALSCALE:GREGORIAN")
                .AppendLine("METHOD:PUBLISH")
                .AppendLine("BEGIN:VEVENT")
                .AppendLine($"UID:event-{ev.EventId}@zoolirante")
                .AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMdd'T'HHmmss'Z'}")
                .AppendLine($"DTSTART:{dtStart}")
                .AppendLine($"DTEND:{dtEnd}")
                .AppendLine($"SUMMARY:{Esc(ev.Title)}")
                .AppendLine($"LOCATION:{Esc(ev.Location)}")
                .AppendLine($"DESCRIPTION:{Esc(ev.Description ?? "")}")
                .AppendLine("END:VEVENT")
                .AppendLine("END:VCALENDAR")
                .ToString();

            var bytes = Encoding.UTF8.GetBytes(ics);
            var safe = string.Join("_", (ev.Title ?? "event").Split(Path.GetInvalidFileNameChars()));
            return File(bytes, "text/calendar", $"{safe}.ics");
        }


// GET: Events/Create
public IActionResult Create()
        {
            return View();
        }

        // POST: Events/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EventId,Title,Description,StartTime,EndTime,Capacity,Price,Location")] Event @event)
        {
            // Basic model validation
            if (!ModelState.IsValid) return View(@event);

            // Business validation
            if (string.IsNullOrWhiteSpace(@event.Title))
                ModelState.AddModelError(nameof(@event.Title), "Title is required.");

            if (string.IsNullOrWhiteSpace(@event.Location))
                ModelState.AddModelError(nameof(@event.Location), "Location is required.");

            if (@event.StartTime >= @event.EndTime)
                ModelState.AddModelError(nameof(@event.EndTime), "End time must be after start time.");

            if (@event.Capacity.HasValue && @event.Capacity.Value < 0)
                ModelState.AddModelError(nameof(@event.Capacity), "Capacity cannot be negative.");

            if (@event.Price < 0)
                ModelState.AddModelError(nameof(@event.Price), "Price cannot be negative.");

            if (!ModelState.IsValid) return View(@event);

            // Success path (this was missing)
            _context.Add(@event);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Events/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events.FindAsync(id);
            if (@event == null)
            {
                return NotFound();
            }
            return View(@event);
        }

        // POST: Events/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EventId,Title,Description,StartTime,EndTime,Capacity,Price,Location")] Event @event)
        {
            if (id != @event.EventId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(@event);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventExists(@event.EventId))
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
            return View(@event);
        }

        // GET: Events/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events
                .FirstOrDefaultAsync(m => m.EventId == id);
            if (@event == null)
            {
                return NotFound();
            }

            return View(@event);
        }

        // POST: Events/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var @event = await _context.Events.FindAsync(id);
            if (@event != null)
            {
                _context.Events.Remove(@event);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.EventId == id);
        }
    }
}
