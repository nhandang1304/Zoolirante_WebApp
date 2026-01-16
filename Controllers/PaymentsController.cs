 using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zoolirante_Open_Minded.Models;
using Zoolirante_Open_Minded.Services;
using Zoolirante_Open_Minded.ViewModels;

namespace Zoolirante_Open_Minded.Controllers
{
	public class PaymentsController : Controller
	{
		private readonly ZooliranteDatabaseContext _context;
		private readonly IEmailService _email;

		public PaymentsController(ZooliranteDatabaseContext context, IEmailService email)
		{
			_context = context;
			_email = email;
		}

		// GET: /Payments/Card
		[HttpGet]
		public IActionResult Card(
			int orderId = 0, string? purpose = null,
			int? ticketId = null, int? qty = null,
			DateTime? visitDate = null, string? ticketType = null)
		{
			var vm = new CardPaymentVM
			{
				OrderId = orderId,
				Purpose = purpose,
				TicketId = ticketId,
				Qty = qty ?? 1,
				VisitDate = visitDate,
				TicketType = string.IsNullOrWhiteSpace(ticketType) ? "Adult" : ticketType
			};
			return View(vm);
		}

		// POST: /Payments/Card
		[HttpPost("/Payments/Card")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Card(CardPaymentVM vm)
		{
			TempData["HitPost"] = "HIT Payments/Card POST";
			if (!ModelState.IsValid) return View(vm);

			// Clean + Luhn
			vm.CardNumber = new string(vm.CardNumber.Where(char.IsDigit).ToArray());
			if (!IsLuhnValid(vm.CardNumber))
			{
				ModelState.AddModelError(nameof(vm.CardNumber), "Invalid card number");
				return View(vm);
			}

			// Expiry
			var lastDay = DateTime.DaysInMonth(vm.ExpYear, vm.ExpMonth);
			var expDate = new DateTime(vm.ExpYear, vm.ExpMonth, lastDay, 23, 59, 59, DateTimeKind.Utc);
			if (expDate < DateTime.UtcNow)
			{
				ModelState.AddModelError(nameof(vm.ExpMonth), "Card expired");
				return View(vm);
			}

			// MEMBERSHIP
			if (string.Equals(vm.Purpose, "membership", StringComparison.OrdinalIgnoreCase))
			{
				vm.TicketType = (vm.TicketType?.Equals("Child", StringComparison.OrdinalIgnoreCase) == true)
					? "Child" : "Adult";

				var type = vm.TicketType;
				decimal unit = (type == "Child") ? 20m : 30m;
				var uid = HttpContext.Session.GetInt32("UserId");
				if (uid is null) return RedirectToAction("Login", "Users");

				var now = DateTime.UtcNow;
				var current = await _context.Memberships
					.Where(x => x.UserId == uid.Value && x.EndDate > now)
					.OrderByDescending(x => x.EndDate)
					.FirstOrDefaultAsync();

				ModelState.Remove(nameof(vm.TicketType));
				ModelState.Remove(nameof(vm.TicketId));
				ModelState.Remove(nameof(vm.Qty));
				ModelState.Remove(nameof(vm.VisitDate));

				if (current is null)
				{
					_context.Memberships.Add(new Membership { UserId = uid.Value, StartDate = now, EndDate = now.AddMonths(1) });
				}
				else
				{
					current.EndDate = current.EndDate.AddMonths(1);
					_context.Update(current);
				}
				await _context.SaveChangesAsync();

				
                var membership = await _context.Memberships
                   .Where(u => u.UserId == uid)
                   .OrderByDescending(m => m.EndDate)
                   .FirstOrDefaultAsync();

                var user = _context.Users.Where(u => u.UserId == uid).FirstOrDefault();

                await _email.BenefitsMembership(user, membership);

                HttpContext.Session.SetString("IsMember", "1");
				TempData["Msg"] = "Payment accepted (demo). Membership activated! Redirecting to Home...";
				vm.CardNumber = vm.Cvv = "";
				return View(vm);
			}

			// ===== TICKET =====
			if (string.Equals(vm.Purpose, "ticket", StringComparison.OrdinalIgnoreCase))
			{
				if (string.IsNullOrWhiteSpace(vm.TicketType))
					vm.TicketType = "Adult";

				var uid = HttpContext.Session.GetInt32("UserId");
				if (uid is null) return RedirectToAction("Login", "Users");

				var qty = Math.Max(1, vm.Qty ?? 1);
				var type = vm.TicketType;

				decimal unit = string.Equals(type, "Child", StringComparison.OrdinalIgnoreCase) ? 20m : 30m;

				
				var isMember = await _context.Memberships
					.AnyAsync(m => m.UserId == uid.Value && m.EndDate >= DateTime.Now);
				if (isMember) unit = Math.Round(unit * 0.80m, 2);

				
				var now = DateTime.Now;

				var ticket = new EntranceTicket
				{
					UserId = uid.Value,               
					Type = type!,
					Price = unit * qty,
					CreatedAt = now,
                    VisitDate = vm.VisitDate ?? now,
                    //ExpiredAt = now.AddMonths(1),       
                    Details = isMember
						? "Ticket purchased online (20% member discount applied)"
						: "Ticket purchased online"
				};

				_context.EntranceTicket.Add(ticket);
				await _context.SaveChangesAsync();

                var visitDate = vm.VisitDate ?? now;
                var user = await _context.Users
     .Include(u => u.AnimalFavourites)
         .ThenInclude(af => af.Animal) 
     .FirstOrDefaultAsync(u => u.UserId == uid.Value);
                await _email.SendTicketConfirmationAsync(user, ticket);
                if (user != null)
                {
                    var daysUntilVisit = (visitDate.Date - now.Date).Days;

                    if (daysUntilVisit <= 1)
                    {
                        var favouriteAnimals = user.AnimalFavourites
                            .Where(a => a.Animal != null)
                                   .Select(a => a.Animal.Name)
                                   .ToList();

                        await _email.BookingReminder(_context, user, ticket, favouriteAnimals);
                    }
                    else
                    {
                        var scheduleDate = visitDate.AddDays(-1).Date + new TimeSpan(10, 0, 0);
                        _context.PendingEmails.Add(new PendingEmail
                        {
                            UserId = user.UserId,
                            TicketId = ticket.TicketId,
                            ScheduledTime = scheduleDate,
                            Sent = false
                        });
                        await _context.SaveChangesAsync();
                    }
                }


                TempData["Msg"] = "Payment accepted (demo). Your ticket has been successfully booked ! Redirecting to Home...";
                vm.CardNumber = vm.Cvv = "";
                return View(vm);
            }

            // MERCHANDISE
            if (vm.OrderId > 0)
			{
				var uid = HttpContext.Session.GetInt32("UserId");
				if (uid is null) return RedirectToAction("Login", "Users");

				var order = await _context.Orders
					.Include(o => o.OrderItems)
					.FirstOrDefaultAsync(o => o.OrderId == vm.OrderId && o.UserId == uid.Value);

				if (order != null)
				{
					
					order.Status = "Paid";

					
					var productIds = order.OrderItems.Select(oi => oi.ProductId).Distinct().ToList();
					var products = await _context.Merchandises
						.Where(m => productIds.Contains(m.ProductId))
						.ToDictionaryAsync(m => m.ProductId);

					
					foreach (var item in order.OrderItems)
					{
						if (products.TryGetValue(item.ProductId, out var p))
						{
							
							var newStock = p.Stock - item.Quantity;
							p.Stock = newStock < 0 ? 0 : newStock;
						}
					}

					await _context.SaveChangesAsync();

					
					HttpContext.Session.Remove("CART_V1");

					
					TempData["Msg"] = "Payment accepted (demo). Order paid, stock updated and cart cleared! Redirecting to Home...";
					vm.CardNumber = vm.Cvv = "";
					return View(vm);
				}
			}

			TempData["Msg"] = "Payment info accepted (demo). No real charge.";
			vm.CardNumber = vm.Cvv = "";
			return View(vm);
		}


		private static bool IsLuhnValid(string digits)
		{
			int sum = 0; bool alt = false;
			for (int i = digits.Length - 1; i >= 0; i--)
			{
				if (!char.IsDigit(digits[i])) return false;
				int n = digits[i] - '0';
				if (alt) { n *= 2; if (n > 9) n -= 9; }
				sum += n; alt = !alt;
			}
			return sum % 10 == 0;
		}

		
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> PaypalDemo(
			int orderId = 0, string? purpose = null,
			int? ticketId = null, int? qty = null,
			DateTime? visitDate = null, string? ticketType = null)
		{
			
			TempData["HitPost"] = "HIT Payments/PaypalDemo POST";

			// ===== MEMBERSHIP =====
			if (string.Equals(purpose, "membership", StringComparison.OrdinalIgnoreCase))
			{
				var uid = HttpContext.Session.GetInt32("UserId");
				if (uid is null) return RedirectToAction("Login", "Users");
                var user = _context.Users.Where(u => u.UserId == uid).FirstOrDefault();
                var now = DateTime.UtcNow;
				var current = await _context.Memberships
					.Where(x => x.UserId == uid.Value && x.EndDate > now)
					.OrderByDescending(x => x.EndDate)
					.FirstOrDefaultAsync();

                
                if (current is null)
					_context.Memberships.Add(new Membership { UserId = uid.Value, StartDate = now, EndDate = now.AddMonths(1) });
				else
				{
					current.EndDate = current.EndDate.AddMonths(1);
					_context.Update(current);
				}

                await _context.SaveChangesAsync();
                var membership = await _context.Memberships
                    .Where(u => u.UserId == uid)
                    .OrderByDescending(m => m.EndDate)
                    .FirstOrDefaultAsync();

                
                await _email.BenefitsMembership(user, membership);
                
                HttpContext.Session.SetString("IsMember", "1");
				TempData["Msg"] = "PayPal (demo): Membership activated/extended!";
				return RedirectToAction("Index", "Memberships");
			}

			// ===== TICKET =====
			if (string.Equals(purpose, "ticket", StringComparison.OrdinalIgnoreCase))
			{
				var uid = HttpContext.Session.GetInt32("UserId");
				if (uid is null) return RedirectToAction("Login", "Users");

				var qtyVal = Math.Max(1, qty ?? 1);
				var type = string.Equals(ticketType, "Child", StringComparison.OrdinalIgnoreCase) ? "Child" : "Adult";
				decimal unit = (type == "Child") ? 20m : 30m;

				var isMember = await _context.Memberships
					.AnyAsync(m => m.UserId == uid.Value && m.EndDate >= DateTime.Now);
				if (isMember) unit = Math.Round(unit * 0.80m, 2);

				var now = DateTime.Now;
				var ticket = new EntranceTicket
				{
					UserId = uid.Value,
					Type = type,
					Price = unit * qtyVal,
					CreatedAt = now,
					VisitDate = visitDate ?? now,
					Details = isMember
						? "Ticket purchased via PayPal (20% member discount)"
						: "Ticket purchased via PayPal"
				};
				_context.EntranceTicket.Add(ticket);
				await _context.SaveChangesAsync();

				var user = await _context.Users.FindAsync(uid.Value);
				if (user != null) await _email.SendTicketConfirmationAsync(user, ticket);

				TempData["Msg"] = "PayPal: Ticket booked successfully!";
				return RedirectToAction("History", "EntranceTickets");
			}

			// ===== MERCHANDISE =====
			if (orderId > 0)
			{
				var uid = HttpContext.Session.GetInt32("UserId");
				if (uid is null) return RedirectToAction("Login", "Users");

				var order = await _context.Orders
					.Include(o => o.OrderItems)
					.FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == uid.Value);

				if (order != null)
				{
					order.Status = "Paid";

					var productIds = order.OrderItems.Select(oi => oi.ProductId).Distinct().ToList();
					var products = await _context.Merchandises
						.Where(m => productIds.Contains(m.ProductId))
						.ToDictionaryAsync(m => m.ProductId);

					foreach (var item in order.OrderItems)
						if (products.TryGetValue(item.ProductId, out var p))
							p.Stock = Math.Max(0, p.Stock - item.Quantity);

					await _context.SaveChangesAsync();

					HttpContext.Session.Remove("CART_V1");
					TempData["Msg"] = "PayPal: Order paid, stock updated, and cart cleared!";
					return RedirectToAction("Index", "Orders");
				}
			}

			TempData["Msg"] = "PayPal (demo): No operation performed.";
			return RedirectToAction("Index", "Home");
		}

	}
}
