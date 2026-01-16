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
    public class OrdersController : Controller
    {
        private readonly ZooliranteDatabaseContext _context;

        public OrdersController(ZooliranteDatabaseContext context)
        {
            _context = context;
        }

        // GET: Orders
        public async Task<IActionResult> Index(string? search)
        {
            ViewData["BannerText"] = "Manage your orders";

			var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("Role");

            var ordersQuery = _context.Orders
                .Include(o => o.PickupLocation)
                .Include(o => o.User)
                    .ThenInclude(u => u.UserDetail)   
                .AsQueryable();


            if (!string.IsNullOrWhiteSpace(search))
            {
                ordersQuery = ordersQuery.Where(o =>o.User.FullName.Contains(search) || o.Items.Contains(search) || o.Status.Contains(search) || o.User.Email.Contains(search));
            }
            if (userRole == "Customer")
            {
                var orders = ordersQuery.Where(o => o.UserId == userId);
                return View(await orders.ToListAsync());
            }

            
            return View(await ordersQuery.ToListAsync());



        }


        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.PickupLocation)
                .Include(o => o.User)
                    .ThenInclude(u => u.UserDetail)   // <-- load Phone, etc.
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Orders/Create
        // GET: Orders/Create
        public IActionResult Create()
        {
            // Populate dropdowns
            var locations = _context.PickupLocations.ToList();
            ViewBag.PickupLocations = new SelectList(locations, "PickupLocationId", "Name");

            var users = _context.Users.ToList();
            ViewBag.Users = new SelectList(users, "UserId", "Email");

            return View();
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,PickupLocationId,PickupDate,TotalAmount")] Order order)
        {
            if (ModelState.IsValid)
            {

                order.OrderDate = DateTime.Now;
                order.Status = "Pending";

                _context.Add(order);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }


            var locations = _context.PickupLocations.ToList();
            ViewBag.PickupLocations = new SelectList(locations, "PickupLocationId", "Name", order.PickupLocationId);

            var users = _context.Users.ToList();
            ViewBag.Users = new SelectList(users, "UserId", "Email", order.UserId);

            return View(order);
        }


        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            ViewData["PickupLocationId"] = new SelectList(_context.PickupLocations, "PickupLocationId", "PickupLocationId", order.PickupLocationId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Email", order.UserId);
            return View(order);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OrderId,UserId,PickupLocationId,OrderDate,TotalAmount,Status,PickupDate,PickupWindow,TimePickup")] Order order)
        {
            if (id != order.OrderId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.OrderId))
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
            ViewData["PickupLocationId"] = new SelectList(_context.PickupLocations, "PickupLocationId", "PickupLocationId", order.PickupLocationId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Email", order.UserId);
            return View(order);
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.PickupLocation)
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderId == id);
        }
    }
}
