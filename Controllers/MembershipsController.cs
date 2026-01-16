using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Zoolirante_Open_Minded.Helpers;
using Zoolirante_Open_Minded.Models;
using Zoolirante_Open_Minded.Services;
using Zoolirante_Open_Minded.ViewModels;
namespace Zoolirante_Open_Minded.Controllers
{
    public class MembershipsController : Controller
    {
        private readonly ZooliranteDatabaseContext _context;
        private readonly IEmailService _email;
        public MembershipsController(ZooliranteDatabaseContext context, IEmailService email)
        {
            _context = context;
            _email = email;
        }

		[HttpGet]
		public async Task<IActionResult> Join()
		{
			var uid = HttpContext.Session.GetInt32("UserId");
			if (uid is null) return RedirectToAction("Login", "Users");
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Cancel()
		{
			var uid = HttpContext.Session.GetInt32("UserId");
			if (uid is null) return RedirectToAction("Login", "Users");

			var now = DateTime.UtcNow;

			var actives = await _context.Memberships
				.Where(x => x.UserId == uid.Value && x.EndDate > now)
				.ToListAsync();

			if (actives.Count > 0)
			{
				foreach (var m in actives) m.EndDate = now.AddDays(-1); 
				_context.UpdateRange(actives);
				await _context.SaveChangesAsync();
			}

			var cart = HttpContext.Session.GetObject<CartVM>("CART_V1");
			if (cart != null)
			{
				foreach (var it in cart.Items)
				{
					if (it.OriginalPrice <= 0) it.OriginalPrice = it.Price;
					it.Price = it.OriginalPrice; 
				}
				HttpContext.Session.SetObject("CART_V1", cart);
			}

			HttpContext.Session.Remove("IsMember");
			TempData["Message"] = "Your membership has been cancelled.";
			return RedirectToAction(nameof(Index));
		}

		public async Task<IActionResult> Index()
        {
            ViewData["BannerText"] = "Join membership";
			var uid = HttpContext.Session.GetInt32("UserId");
			if (uid is null) return RedirectToAction("Login", "Users");

			var membership = await _context.Memberships
				.Where(m => m.UserId == uid.Value)
				.OrderByDescending(m => m.EndDate)
				.FirstOrDefaultAsync();

			return View(membership);
		}

        // GET: Memberships/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            ViewData["BannerText"] = "Manage your membership";

			if (id == null)
            {
                return NotFound();
            }

            var membership = await _context.Memberships
                .FirstOrDefaultAsync(m => m.MembershipId == id);
            if (membership == null)
            {
                return NotFound();
            }

            return View(membership);
        }

        // GET: Memberships/Create
        public IActionResult Create()
        {
            ViewData["BannerText"] = "Manage your membership";
			var uid = HttpContext.Session.GetInt32("UserId");
			if (uid is null) return RedirectToAction("Login", "Users");
			return View();
        }

        // POST: Memberships/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MembershipId,UserId,StartDate,EndDate")] Membership membership)
        {
            if (ModelState.IsValid)
            {
				var uid = HttpContext.Session.GetInt32("UserId");


                
				if (uid is null) return RedirectToAction("Login", "Users");

				var m = new Membership
				{
					UserId = uid.Value,
					StartDate = DateTime.Now,
					EndDate = DateTime.Now.AddMonths(1)
				};
                
				_context.Add(m);
                

                await _context.SaveChangesAsync();
				TempData["Message"] = "Membership created!";
				return RedirectToAction(nameof(Index));
			}
            return View(membership);
        }

        // GET: Memberships/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var membership = await _context.Memberships.FindAsync(id);
            if (membership == null)
            {
                return NotFound();
            }
            return View(membership);
        }

        // POST: Memberships/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MembershipId,UserId,StartDate,EndDate")] Membership membership)
        {
            if (id != membership.MembershipId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(membership);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MembershipExists(membership.MembershipId))
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
            return View(membership);
        }

        // GET: Memberships/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var membership = await _context.Memberships
                .FirstOrDefaultAsync(m => m.MembershipId == id);
            if (membership == null)
            {
                return NotFound();
            }

            return View(membership);
        }

        // POST: Memberships/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var membership = await _context.Memberships.FindAsync(id);
            if (membership != null)
            {
                _context.Memberships.Remove(membership);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        
        public async Task<IActionResult> ShowDetails(string? search)
        {
            var membership = _context.Memberships.Include(i=> i.User).Where(i => i.EndDate >= DateTime.Now).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                int userIdSearch;
                bool isNumber = int.TryParse(search, out userIdSearch);

                membership = membership.Where(a =>
                    (isNumber && a.UserId == userIdSearch) ||
                    a.User.FullName.Contains(search));
            }

            return View(membership.ToList());
        }
        private bool MembershipExists(int id)
        {
            return _context.Memberships.Any(e => e.MembershipId == id);
        }
    }
}
