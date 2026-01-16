using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Zoolirante_Open_Minded.Models;
using Zoolirante_Open_Minded.Services;


namespace Zoolirante_Open_Minded.Controllers
{
    public class EntranceTicketsController : Controller
    {
        // Which payment methods are valid per ticket type
        private static readonly Dictionary<string, string[]> _allowedPaymentsByType =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Adult"] = new[] { "Visa", "MasterCard", "PayPal" },
                ["Child"] = new[] { "Visa", "MasterCard" },           // PayPal disallowed
                ["Senior"] = new[] { "Visa", "MasterCard", "PayPal" },
                ["Concession"] = new[] { "Visa", "MasterCard" }
            };

        // Fallback for unknown types (no Cash for online)
        private static readonly string[] _defaultOnlinePayments = new[] { "Visa", "MasterCard", "PayPal" };

        private readonly ZooliranteDatabaseContext _context;
        private readonly IEmailService _emailService;

        [HttpGet]
        public IActionResult Index(string? search)
        {
            var animalF = _context.EntranceTicket
                      .Include(a => a.User).AsQueryable();
                      
            if (!string.IsNullOrEmpty(search))
            {
                int userIdSearch;
                bool isNumber = int.TryParse(search, out userIdSearch);
                animalF = animalF.Where(a =>
                    (isNumber && a.UserId == userIdSearch) ||
                    a.User.FullName.Contains(search) || a.Type.Contains(search));
            }

            return View(animalF.ToList());
        }
        public EntranceTicketsController(ZooliranteDatabaseContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }


        public IActionResult Buy()
        {
            ViewData["BannerText"] = "Buy tickets";

			var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Users");

            var ticket = new EntranceTicket
            {
                UserId = userId.Value,
                Type = "Adult",
                Price = 30m
            };
            return View(ticket);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Buy(EntranceTicket ticket)
        {
            ViewData["BannerText"] = "Buy tickets";


			var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Users");

            ticket.UserId = userId.Value; 


            var allowed = _allowedPaymentsByType.TryGetValue(ticket.Type, out var list)
                ? list
                : _defaultOnlinePayments;

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null) return RedirectToAction("Login", "Users");

            if (!allowed.Contains(user.PaymentMethod ?? "", StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty,
                    $"Your saved payment method ({user.PaymentMethod ?? "None"}) is not valid for {ticket.Type} tickets. " +
                    $"Allowed: {string.Join(", ", allowed)}. Please change your payment method.");

                ViewBag.AllowedPayments = allowed;
                ViewBag.UserPaymentMethod = user.PaymentMethod;
                ViewBag.PaymentMethodValid = false;

     
                ticket.Price = ticket.Type == "Child" ? 20m : 30m;

                return View(ticket);
            }

            ticket.Price = ticket.Type == "Child" ? 20m : 30m;

                
                ticket.CreatedAt = DateTime.Now;


			ticket.Price = ticket.Type == "Child" ? 20m : 30m;


			ticket.CreatedAt = DateTime.Now;

                //ticket.ExpiredAt = DateTime.Now.AddMonths(1);
                ticket.Details = "Ticket purchased online";

			var isMember = await _context.Memberships
		.AnyAsync(m => m.UserId == userId.Value && m.EndDate >= DateTime.UtcNow);
			if (isMember)
			{
				ticket.Price = Math.Round(ticket.Price * 0.80m, 2);
				ticket.Details += " (20% member discount applied)";
			}


			_context.EntranceTicket.Add(ticket);
                await _context.SaveChangesAsync();

              
               
            if (user != null)
            {
                
                await _emailService.SendTicketConfirmationAsync(user, ticket);
            }

            
            return RedirectToAction("Buy");

            }

        public async Task<IActionResult> Confirmation(int id)
        {
            var ticket = await _context.EntranceTicket.FindAsync(id);
            if (ticket == null) return NotFound();


            return View(ticket);
        }

		[HttpGet]
		public async Task<IActionResult> History()
		{
            ViewData["BannerText"] = "Your visiting history";
			var userId = HttpContext.Session.GetInt32("UserId");
			if (userId is null) return RedirectToAction("Login", "Users");

			var visits = await _context.EntranceTicket
				.Where(t => t.UserId == userId.Value)
				.OrderByDescending(t => t.CreatedAt) 
				.ToListAsync();

			return View(visits); 
		}

	}
}
