using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zoolirante_Open_Minded.Models;
using Zoolirante_Open_Minded.Services;
using static Zoolirante_Open_Minded.Controllers.UsersController;

namespace Zoolirante_Open_Minded.Controllers
{
    public class UsersController : Controller
    {
        private readonly ZooliranteDatabaseContext _context;
        private readonly IEmailService _emailService;

        // 4.1
        public class ProfileEditVm
        {
            public int UserId { get; set; }
            public string Email { get; set; } = "";
            public string Role { get; set; } = "";

            public string? FirstName { get; set; }
            public string? MiddleName { get; set; }
            public string? LastName { get; set; }
            public string? Phone { get; set; }
            public string? Street { get; set; }
        }

        public UsersController(ZooliranteDatabaseContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }
       

        // GET: Reset password
        [HttpGet]
        public IActionResult Reset() => View();

        // POST: Request password reset
        [HttpPost]
        public async Task<IActionResult> Reset(User model)
        {
            if (string.IsNullOrEmpty(model.Email))
            {
                ModelState.AddModelError("", "Email is required.");
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Email not found.");
                return View(model);
            }
            
            user.ResetToken = Guid.NewGuid().ToString();
            user.TokenExpiry = DateTime.Now.AddHours(1);
            await _context.SaveChangesAsync();

            var resetLink = Url.Action("ConfirmReset", "Users",
                new { token = user.ResetToken, email = user.Email }, Request.Scheme);

            await _emailService.SendEmailAsync(user.Email, "Password Reset",
                $"Click <a href='{resetLink}'>here</a> to reset your password.");

            TempData["Message"] = "Check your email on your laptop for reset instructions.";
            return RedirectToAction("Login");
        }

      
        [HttpGet]
        public async Task<IActionResult> ConfirmReset(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
                return BadRequest("Invalid request.");

            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email == email && u.ResetToken == token && u.TokenExpiry > DateTime.Now);

            if (user == null) return BadRequest("Invalid or expired token.");

            return View(new ResetPasswordViewModel { Email = email, Token = token });
        }

        
        [HttpPost]
        public async Task<IActionResult> ConfirmReset(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email == model.Email && u.ResetToken == model.Token && u.TokenExpiry > DateTime.Now);

            if (user == null) return BadRequest("Invalid or expired token.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            user.ResetToken = null;
            user.TokenExpiry = null;

            await _context.SaveChangesAsync();
            TempData["Message"] = "Password successfully reset. Please login.";
            return RedirectToAction("Login");
        }

       
        [HttpGet]
        public IActionResult Login() => View();

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(User model)
        {
            ViewData["BannerText"] = "Manage your account";

			if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.PasswordHash))
            {
                if (string.IsNullOrWhiteSpace(model.Email))
                    ModelState.AddModelError(nameof(model.Email), "Email is required.");
                if (string.IsNullOrWhiteSpace(model.PasswordHash))
                    ModelState.AddModelError(nameof(model.PasswordHash), "Password is required.");
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null || string.IsNullOrEmpty(user.PasswordHash) ||
                !BCrypt.Net.BCrypt.Verify(model.PasswordHash, user.PasswordHash))
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("Role", user.Role);
            return RedirectToAction("Index", "Home");
        }

        // Logout
        public IActionResult LogOut()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // GET: Edit payment method
        [HttpGet]
        public async Task<IActionResult> EditPayment()
        {
            var uid = HttpContext.Session.GetInt32("UserId");
            if (uid == null) return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(uid.Value);
            if (user == null) return NotFound();

            ViewBag.PaymentOptions = new List<string> { "Visa", "MasterCard", "PayPal", "Cash" };
            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> EditInfo()
        {
            var uid = HttpContext.Session.GetInt32("UserId");
            if (uid == null) return RedirectToAction("Login");

            var user = await _context.Users
                .Include(u => u.UserDetail)
                .FirstOrDefaultAsync(u => u.UserId == uid.Value);
            if (user == null) return NotFound();

            var d = user.UserDetail;
            var vm = new ProfileEditVm
            {
                UserId = user.UserId,
                Email = user.Email,
                Role = user.Role,
                FirstName = d?.FirstName,
                MiddleName = d?.MiddleName,
                LastName = d?.LastName,
                Phone = d?.Phone,
                Street = d?.Street
            };

            return View(vm); 
        }


        // POST: Update payment method
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPayment(string paymentMethod)
        {
            var uid = HttpContext.Session.GetInt32("UserId");
            if (uid == null) return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(uid.Value);
            if (user == null) return NotFound();

            user.PaymentMethod = paymentMethod;
            _context.Update(user);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Payment method updated.";
            return RedirectToAction("Index", "Home");
        }

        // GET: Users list
        public async Task<IActionResult> Index() => View(await _context.Users.ToListAsync());

        // GET: User details
        public async Task<IActionResult> Details(int? id)
        {
            ViewData["BannerText"] = "Manage your account";

			if (id == null) return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) return NotFound();

            return View(user);
        }

        // GET: Create user
        public IActionResult Create() => View();

        // POST: Create user
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user)
        {
            bool emailExists = await _context.Users
        .AnyAsync(u => u.Email == user.Email);

            if (emailExists)
            {
                
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(user);
            }
            //if (ModelState.IsValid)
            //{
            // Hash password before saving
            if (!string.IsNullOrEmpty(user.PasswordHash))
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);

                _context.Add(user);
                await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Account created successfully!";
            return RedirectToAction("Login");
            }
           // return View(user);
        //}

        // GET: Edit user
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: Edit user 4.2
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserId,FullName,Email,PasswordHash,Role,PaymentMethod")] User user)
        {
            if (id != user.UserId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Hash password if changed
                    var existingUser = await _context.Users.FindAsync(id);
                    if (existingUser == null) return NotFound();

                    existingUser.FullName = user.FullName;
                    existingUser.Email = user.Email;
                    existingUser.Role = user.Role;
                    existingUser.PaymentMethod = user.PaymentMethod;

                    if (!string.IsNullOrEmpty(user.PasswordHash))
                        existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);

                    _context.Update(existingUser);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserId)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

       //4.3
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditInfo(ProfileEditVm vm)
        {
            var uid = HttpContext.Session.GetInt32("UserId");
            if (uid == null || uid.Value != vm.UserId) return RedirectToAction("Login");
            if (!ModelState.IsValid) return View(vm);

            var user = await _context.Users
                .Include(u => u.UserDetail)
                .FirstOrDefaultAsync(u => u.UserId == uid.Value);
            if (user == null) return NotFound();

            var detail = user.UserDetail;
            if (detail == null)
            {
                detail = new UserDetail { UserId = user.UserId };
                _context.UserDetails.Add(detail);
                user.UserDetail = detail;
            }

            detail.FirstName = vm.FirstName?.Trim() ?? "";
            detail.MiddleName = string.IsNullOrWhiteSpace(vm.MiddleName) ? null : vm.MiddleName!.Trim();
            detail.LastName = vm.LastName?.Trim() ?? "";
            detail.Phone = string.IsNullOrWhiteSpace(vm.Phone) ? null : vm.Phone!.Trim();
            detail.Street = string.IsNullOrWhiteSpace(vm.Street) ? null : vm.Street!.Trim();


            user.FullName = string.Join(" ",
                new[] { detail.FirstName, detail.MiddleName, detail.LastName }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

            await _context.SaveChangesAsync();


            return RedirectToAction(nameof(Details), new { id = user.UserId });
        }


        // GET: Delete user
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: Delete user
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users
                                     .Include(u => u.Memberships) 
                                     .FirstOrDefaultAsync(u => u.UserId == id);

            if (user != null)
            {
                // Remove related memberships first
                if (user.Memberships != null && user.Memberships.Any())
                {
                    _context.Memberships.RemoveRange(user.Memberships);
                }

                // Remove user
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }


        private bool UserExists(int id) => _context.Users.Any(u => u.UserId == id);
    }
}

