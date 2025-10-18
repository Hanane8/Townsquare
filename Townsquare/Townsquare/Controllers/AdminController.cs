using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Townsquare.Data;
using Townsquare.Models;

namespace Townsquare.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Admin
        public async Task<IActionResult> Index()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var totalEvents = await _context.Events.CountAsync();
            var totalRsvps = await _context.RSVPs.CountAsync();
            var orphanedEvents = await _context.Events
                .CountAsync(e => e.CreatedById == null);

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalEvents = totalEvents;
            ViewBag.TotalRsvps = totalRsvps;
            ViewBag.OrphanedEvents = orphanedEvents;

            return View();
        }

        // GET: Admin/ManageUsers
        public async Task<IActionResult> ManageUsers(string searchString)
        {
            var users = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                users = users.Where(u =>
                    u.FullName.Contains(searchString) ||
                    u.Email.Contains(searchString));
            }

            var userList = await users
                .OrderBy(u => u.FullName)
                .ToListAsync();

            // Get roles for each user
            var userRoles = new Dictionary<string, IList<string>>();
            foreach (var user in userList)
            {
                userRoles[user.Id] = await _userManager.GetRolesAsync(user);
            }

            ViewBag.UserRoles = userRoles;
            ViewBag.SearchString = searchString;

            return View(userList);
        }

        // GET: Admin/UserDetails/5
        public async Task<IActionResult> UserDetails(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var eventsCreated = await _context.Events
                .Where(e => e.CreatedById == id)
                .CountAsync();
            var rsvpsCount = await _context.RSVPs
                .Where(r => r.UserId == id)
                .CountAsync();

            ViewBag.Roles = roles;
            ViewBag.EventsCreated = eventsCreated;
            ViewBag.RsvpsCount = rsvpsCount;

            return View(user);
        }

        // POST: Admin/DeleteUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);

            // Prevent admin from deleting themselves
            if (id == currentUserId)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(ManageUsers));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Mark user's events as orphaned (set CreatedById to null)
            var userEvents = await _context.Events
                .Where(e => e.CreatedById == id)
                .ToListAsync();

            foreach (var evt in userEvents)
            {
                evt.CreatedById = null;
            }

            // Delete user's RSVPs
            var userRsvps = await _context.RSVPs
                .Where(r => r.UserId == id)
                .ToListAsync();

            _context.RSVPs.RemoveRange(userRsvps);

            // Delete user's notifications
            var userNotifications = await _context.Notifications
                .Where(n => n.RecipientUserId == id)
                .ToListAsync();

            _context.Notifications.RemoveRange(userNotifications);

            // Delete the user
            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = $"User {user.FullName} has been deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to delete user.";
            }

            return RedirectToAction(nameof(ManageUsers));
        }

        // GET: Admin/OrphanedEvents
        public async Task<IActionResult> OrphanedEvents()
        {
            var orphanedEvents = await _context.Events
                .Where(e => e.CreatedById == null)
                .Include(e => e.RSVPs)
                .OrderByDescending(e => e.StartUtc)
                .ToListAsync();

            return View(orphanedEvents);
        }

        // POST: Admin/DeleteOrphanedEvent/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOrphanedEvent(int id)
        {
            var evt = await _context.Events
                .Include(e => e.RSVPs)
                .FirstOrDefaultAsync(e => e.Id == id && e.CreatedById == null);

            if (evt == null)
            {
                return NotFound();
            }

            // Delete associated RSVPs
            _context.RSVPs.RemoveRange(evt.RSVPs);

            // Delete the event
            _context.Events.Remove(evt);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Event '{evt.Title}' has been deleted.";
            return RedirectToAction(nameof(OrphanedEvents));
        }

        // POST: Admin/ReassignEvent/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReassignEvent(int eventId, string newOwnerId)
        {
            var evt = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == eventId && e.CreatedById == null);

            if (evt == null)
            {
                return NotFound();
            }

            var newOwner = await _userManager.FindByIdAsync(newOwnerId);
            if (newOwner == null)
            {
                TempData["Error"] = "Invalid user selected.";
                return RedirectToAction(nameof(OrphanedEvents));
            }

            evt.CreatedById = newOwnerId;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Event '{evt.Title}' has been reassigned to {newOwner.FullName}.";
            return RedirectToAction(nameof(OrphanedEvents));
        }

        // GET: Admin/ManageRoles/5
        public async Task<IActionResult> ManageRoles(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.ToListAsync();

            ViewBag.UserRoles = userRoles;
            ViewBag.AllRoles = allRoles;
            ViewBag.User = user;

            return View();
        }

        // POST: Admin/AddRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRole(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                TempData["Error"] = "Role does not exist.";
                return RedirectToAction(nameof(ManageRoles), new { id = userId });
            }

            var result = await _userManager.AddToRoleAsync(user, roleName);

            if (result.Succeeded)
            {
                TempData["Success"] = $"Role '{roleName}' added to {user.FullName}.";
            }
            else
            {
                TempData["Error"] = "Failed to add role.";
            }

            return RedirectToAction(nameof(ManageRoles), new { id = userId });
        }

        // POST: Admin/RemoveRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRole(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);

            // Prevent admin from removing their own Admin role
            if (userId == currentUserId && roleName == "Admin")
            {
                TempData["Error"] = "You cannot remove your own Admin role.";
                return RedirectToAction(nameof(ManageRoles), new { id = userId });
            }

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);

            if (result.Succeeded)
            {
                TempData["Success"] = $"Role '{roleName}' removed from {user.FullName}.";
            }
            else
            {
                TempData["Error"] = "Failed to remove role.";
            }

            return RedirectToAction(nameof(ManageRoles), new { id = userId });
        }

        // GET: Admin/EventStatistics
        public async Task<IActionResult> EventStatistics()
        {
            var eventsByCategory = await _context.Events
                .GroupBy(e => e.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            var upcomingEvents = await _context.Events
                .CountAsync(e => e.StartUtc >= DateTime.UtcNow);

            var pastEvents = await _context.Events
                .CountAsync(e => e.StartUtc < DateTime.UtcNow);

            var mostPopularEvent = await _context.Events
                .Include(e => e.RSVPs)
                .OrderByDescending(e => e.RSVPs.Count)
                .FirstOrDefaultAsync();

            ViewBag.EventsByCategory = eventsByCategory;
            ViewBag.UpcomingEvents = upcomingEvents;
            ViewBag.PastEvents = pastEvents;
            ViewBag.MostPopularEvent = mostPopularEvent;

            return View();
        }
    }
}