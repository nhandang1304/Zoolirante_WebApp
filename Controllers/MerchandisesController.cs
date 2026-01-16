using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zoolirante_Open_Minded.Helpers;
using Zoolirante_Open_Minded.Models;
using Zoolirante_Open_Minded.ViewModels;

namespace Zoolirante_Open_Minded.Controllers
{
    public class MerchandisesController : Controller
    {
        private readonly ZooliranteDatabaseContext _context;

        public MerchandisesController(ZooliranteDatabaseContext context)
        {
            _context = context;
        }

        // GET: Merchandises
        public async Task<IActionResult> Index(string? searchText, string? category)
        {
            ViewData["BannerText"] = "View our merchandises";

            var q = _context.Merchandises.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
                q = q.Where(i => i.Name.StartsWith(searchText));

            var categories = await _context.Merchandises
                .AsNoTracking()
                .Where(m => !string.IsNullOrEmpty(m.Category))
                .Select(m => m.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var categoryItems = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "All categories" }
            };
            categoryItems.AddRange(categories.Select(c => new SelectListItem
            {
                Value = c,
                Text = c,
                Selected = string.Equals(c, category)
            }));
            ViewBag.CategoryList = categoryItems;

            if (!string.IsNullOrWhiteSpace(category))
                q = q.Where(i => i.Category == category);

            return View(await q.OrderBy(i => i.Name).ToListAsync());
        }

        // GET: Merchandises/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var merchandise = await _context.Merchandises
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (merchandise == null) return NotFound();

            return View(merchandise);
        }

        // POST: Merchandises/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int id, int qty = 1)
        {
            // product
            var p = await _context.Merchandises.FirstOrDefaultAsync(x => x.ProductId == id);
            if (p == null) return NotFound();

            // membership for discount
            var now = DateTime.UtcNow;
            var uid = HttpContext.Session.GetInt32("UserId");
            var isMember = uid.HasValue && await _context.Memberships
                .AnyAsync(m => m.UserId == uid.Value && m.EndDate > now);

            // cart
            const string CartKey = "CART_V1";
            var cart = HttpContext.Session.GetObject<CartVM>(CartKey) ?? new CartVM();

            // compute price: special first, then member 10%
            decimal basePrice = (p.SpecialPrice.HasValue && p.SpecialQty > 0)
                ? p.SpecialPrice.Value
                : p.Price;
            decimal unitPrice = isMember ? basePrice * 0.90m : basePrice;
            unitPrice = Math.Round(unitPrice, 2);

            // add/update line
            var line = cart.Items.FirstOrDefault(i => i.ProductId == id);
            if (line == null)
            {
                cart.Items.Add(new CartItemVM
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    OriginalPrice = p.Price,
                    Price = unitPrice,
                    Qty = Math.Max(1, Math.Min(qty, p.Stock)),
                    ImageUrl = p.ImageUrl
                });
            }
            else
            {
                var max = Math.Max(1, p.Stock);
                line.Qty = Math.Min(max, line.Qty + Math.Max(1, qty));
                line.OriginalPrice = p.Price;

                // recalc unit price on update as well
                decimal basePrice2 = (p.SpecialPrice.HasValue && p.SpecialQty > 0)
                    ? p.SpecialPrice.Value
                    : p.Price;
                line.Price = Math.Round(isMember ? basePrice2 * 0.90m : basePrice2, 2);
            }

            // persist cart
            HttpContext.Session.SetObject(CartKey, cart);

            // toast
            TempData["CartMessage"] = isMember
                ? $"Added {p.Name} (x{qty}) — 10% member discount applied"
                : $"Added {p.Name} (x{qty})";

            // go back where user came from
            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrWhiteSpace(referer)) return Redirect(referer);
            return RedirectToAction(nameof(Index));
        }

        // POST: Merchandises/UpdateSpecial
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSpecial(int id, decimal? specialPrice, int specialQty, string? specialReason, decimal? percent)
        {
            var m = await _context.Merchandises.FindAsync(id);
            if (m == null) return NotFound();

            // If percent provided, compute price from original
            if (percent.HasValue && percent.Value >= 0)
            {
                var computed = Math.Round(m.Price * (1m - (percent.Value / 100m)), 2);
                specialPrice = computed;
            }

            // Save fields (null clears the special)
            m.SpecialPrice = (specialPrice.HasValue && specialPrice.Value >= 0) ? specialPrice : null;
            m.SpecialQty = Math.Max(0, specialQty);
            m.SpecialReason = string.IsNullOrWhiteSpace(specialReason) ? null : specialReason.Trim();

            await _context.SaveChangesAsync();
            TempData["Msg"] = "Special price updated.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Merchandises/Create
        public IActionResult Create() => View();

        // POST: Merchandises/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductId,Name,Description,Price,Stock,ImageUrl,Category")] Merchandise merchandise)
        {
            if (!ModelState.IsValid) return View(merchandise);

            _context.Add(merchandise);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Merchandises/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var merchandise = await _context.Merchandises.FindAsync(id);
            if (merchandise == null) return NotFound();

            return View(merchandise);
        }

        // POST: Merchandises/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,Name,Description,Price,Stock,ImageUrl,Category")] Merchandise merchandise)
        {
            if (id != merchandise.ProductId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(merchandise);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MerchandiseExists(merchandise.ProductId)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(merchandise);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustStock(int id, int adjustBy = 0, string? reason = null)
        {
            var p = await _context.Merchandises.FindAsync(id);
            if (p == null) return NotFound();

            // adjust SOH; never below zero
            p.Stock = Math.Max(0, p.Stock + adjustBy);

            await _context.SaveChangesAsync();

            TempData["Toast"] = $"SOH adjusted by {adjustBy}. New SOH: {p.Stock}.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        // GET: Merchandises/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var merchandise = await _context.Merchandises
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (merchandise == null) return NotFound();

            return View(merchandise);
        }

        // POST: Merchandises/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var merchandise = await _context.Merchandises.FindAsync(id);
            if (merchandise != null)
            {
                _context.Merchandises.Remove(merchandise);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool MerchandiseExists(int id) =>
            _context.Merchandises.Any(e => e.ProductId == id);
    }
}
