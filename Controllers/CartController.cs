using Microsoft.AspNetCore.Mvc;
using Zoolirante_Open_Minded.ViewModels;
using Zoolirante_Open_Minded.Helpers;
using Zoolirante_Open_Minded.Models;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;


namespace Zoolirante_Open_Minded.Controllers
{
    public class CartController : Controller
    {
        private const string CartKey = "CART_V1";
        private readonly ZooliranteDatabaseContext _db;


        public CartController(ZooliranteDatabaseContext db)
        {
            _db = db;
        }

        private async Task<List<SelectListItem>> LoadPickupOptionsAsync()
        {
            var items = new List<SelectListItem>();
            var conn = _db.Database.GetDbConnection();

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        SELECT PickupLocationId, Name
        FROM dbo.PickupLocations
        WHERE IsActive = 1
        ORDER BY Name";
            using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);

            while (await reader.ReadAsync())
            {
                items.Add(new SelectListItem
                {
                    Value = reader.GetInt32(0).ToString(),
                    Text = reader.GetString(1)
                });
            }
            return items;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {

            var cart = HttpContext.Session.GetObject<CartVM>(CartKey) ?? new CartVM();

            var uid = HttpContext.Session.GetInt32("UserId");
            var isMember = false;
            if (uid.HasValue)
            {
                var now = DateTime.UtcNow;
                isMember = await _db.Memberships.AnyAsync(m => m.UserId == uid.Value && m.EndDate > now);
            }

            foreach (var it in cart.Items)
            {
                var p = await _db.Merchandises
                    .Where(m => m.ProductId == it.ProductId)
                    .Select(m => new { m.Price, m.SpecialPrice, m.SpecialQty })
                    .FirstOrDefaultAsync();

                // regular price from DB (fallback to the line’s current price)
                var regular = p?.Price ?? it.Price;

                // how many units on this line can use special price?
                var specialCap = (p?.SpecialPrice.HasValue == true && p.SpecialQty > 0)
                    ? Math.Min(it.Qty, p.SpecialQty)
                    : 0;

                var memFactor = isMember ? 0.90m : 1.00m;
                var unitReg = Math.Round(regular * memFactor, 2);
                var unitSpec = Math.Round(((p?.SpecialPrice) ?? regular) * memFactor, 2);

                // total for the line with a mix of special + regular units
                var lineTotal = (specialCap * unitSpec) + ((it.Qty - specialCap) * unitReg);

                // keep OriginalPrice as the REGULAR price for reference
                it.OriginalPrice = regular;

                // store an EFFECTIVE unit price so (Price * Qty) == correct total
                it.Price = it.Qty > 0 ? Math.Round(lineTotal / it.Qty, 2) : unitReg;
            }
            HttpContext.Session.SetObject(CartKey, cart);



            return View(cart);
        }


        public async Task<IActionResult> Checkout()
        {
            var cart = HttpContext.Session.GetObject<CartVM>(CartKey) ?? new CartVM();
            ViewBag.PickupOptions = await LoadPickupOptionsAsync(); // for the dropdown
            return View(cart); // Views/Cart/Checkout.cshtml
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPickup(int id)
        {
            var cart = HttpContext.Session.GetObject<CartVM>(CartKey) ?? new CartVM();


            string? name = null;
            var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
            SELECT Name
            FROM dbo.PickupLocations
            WHERE PickupLocationId = @id AND IsActive = 1";
                var p = cmd.CreateParameter();
                p.ParameterName = "@id";
                p.Value = id;
                cmd.Parameters.Add(p);

                var result = await cmd.ExecuteScalarAsync();
                name = result as string;
            }
            await conn.CloseAsync();

            if (name == null) return NotFound();

            cart.PickupLocationId = id;
            cart.PickupLocationName = name;
            HttpContext.Session.SetObject(CartKey, cart);

            TempData["CartMessage"] = $"Pickup location set: {name}";
            return RedirectToAction(nameof(Checkout));
        }



        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Update(int productId, int qty)
        {
            var cart = HttpContext.Session.GetObject<CartVM>(CartKey) ?? new CartVM();
            var line = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (line == null) return RedirectToAction(nameof(Index));

            qty = Math.Max(0, qty);
            if (qty == 0)
            {
                cart.Items.Remove(line);
            }
            else
            {
                line.Qty = qty;   // no DB mutation here
            }

            HttpContext.Session.SetObject(CartKey, cart);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Remove(int productId)
        {
            var cart = HttpContext.Session.GetObject<CartVM>(CartKey) ?? new CartVM();
            cart.Items.RemoveAll(i => i.ProductId == productId);
            HttpContext.Session.SetObject(CartKey, cart);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            HttpContext.Session.SetObject(CartKey, new CartVM());
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,PickupLocationId,PickupDate")] Order order)
        {
            var cart = HttpContext.Session.GetObject<CartVM>(CartKey) ?? new CartVM();
            if (!cart.Items.Any())
            {
                TempData["CartMessage"] = "Your cart is empty.";
                return RedirectToAction(nameof(Checkout));
            }
            
            var uid2 = HttpContext.Session.GetInt32("UserId");
            var isMember2 = false;
            if (uid2.HasValue)
            {
                var now2 = DateTime.UtcNow;
                isMember2 = await _db.Memberships.AnyAsync(m => m.UserId == uid2.Value && m.EndDate > now2);
            }

            foreach (var it in cart.Items)
            {
                var p2 = await _db.Merchandises
                    .Where(m => m.ProductId == it.ProductId)
                    .Select(m => new { m.Price, m.SpecialPrice, m.SpecialQty })
                    .FirstOrDefaultAsync();

                var regular2 = p2?.Price ?? it.Price;
                var specialCap = (p2?.SpecialPrice.HasValue == true && p2.SpecialQty > 0)
                    ? Math.Min(it.Qty, p2.SpecialQty)
                    : 0;

                var memFactor2 = isMember2 ? 0.90m : 1.00m;
                var unitReg2 = Math.Round(regular2 * memFactor2, 2);
                var unitSpec2 = Math.Round(((p2?.SpecialPrice) ?? regular2) * memFactor2, 2);

                var lineTotal2 = (specialCap * unitSpec2) + ((it.Qty - specialCap) * unitReg2);

                it.OriginalPrice = regular2;
                it.Price = it.Qty > 0 ? Math.Round(lineTotal2 / it.Qty, 2) : unitReg2;
            }
            HttpContext.Session.SetObject(CartKey, cart);

            // Optional: basic validation that a pickup location exists in the cart, etc.
            // if (cart.PickupLocationId == null) { ... }

            // Start transaction to keep stock/capacity + order write atomic
            await using var tx = await _db.Database.BeginTransactionAsync();

            // 1) Re-check availability and deduct
            foreach (var line in cart.Items)
            {
                var p = await _db.Merchandises
                    .FirstOrDefaultAsync(x => x.ProductId == line.ProductId);

                if (p == null)
                {
                    ModelState.AddModelError("", $"Item not found: {line.ProductId}");
                    await tx.RollbackAsync();
                    return View("Checkout", cart);
                }

                if (p.Stock < line.Qty)
                {
                    ModelState.AddModelError("", $"Only {p.Stock} left for {p.Name}.");
                    await tx.RollbackAsync();
                    return View("Checkout", cart);
                }

                p.Stock -= line.Qty;

              
                if (p.CurrentShelf > 0)
                {
                    var shelfTake = Math.Min(line.Qty, p.CurrentShelf);
                    p.CurrentShelf -= shelfTake;  // clamp by Math.Min
                }
            }

            // 2) Persist deductions
            await _db.SaveChangesAsync();
            
            order.Items = string.Join(", ", cart.Items.Select(i => $"({i.Qty}) {i.Name}"));
            order.TotalAmount = cart.Subtotal;
            order.OrderDate = DateTime.Now;
            order.Status = "Pending";

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            // 4) Commit transaction and clear the cart
            await tx.CommitAsync();
            HttpContext.Session.SetObject(CartKey, new CartVM());

            TempData["CartMessage"] = $"Order placed successfully! Total: {order.TotalAmount:C}";
            return RedirectToAction("Index", "Orders");
        }


    }
}
