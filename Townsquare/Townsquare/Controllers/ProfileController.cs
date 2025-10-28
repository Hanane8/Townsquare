using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Townsquare.Data;
using Townsquare.Models;

namespace Townsquare.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public ProfileController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Profile
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            // Get user's created events count
            var createdEventsCount = await _context.Events
                .CountAsync(e => e.CreatedById == userId);

            // Get user's RSVPs count
            var rsvpCount = await _context.RSVPs
                .CountAsync(r => r.UserId == userId);

            // Get unread notifications count
            var unreadNotifications = await _context.Notifications
                .CountAsync(n => n.RecipientUserId == userId && !n.IsRead);

            ViewBag.CreatedEventsCount = createdEventsCount;
            ViewBag.RsvpCount = rsvpCount;
            ViewBag.UnreadNotifications = unreadNotifications;

            return View(user);
        }

        // GET: Profile/MyEvents
        public async Task<IActionResult> MyEvents()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var myEvents = await _context.Events
                .Where(e => e.CreatedById == userId)
                .Include(e => e.RSVPs)
                .OrderByDescending(e => e.StartUtc)
                .ToListAsync();

            return View(myEvents);
        }

        // GET: Profile/MyRsvps
        public async Task<IActionResult> MyRsvps()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var myRsvps = await _context.RSVPs
                .Where(r => r.UserId == userId)
                .Include(r => r.Event)
                    .ThenInclude(e => e.CreatedBy)
                .OrderBy(r => r.Event.StartUtc)
                .ToListAsync();

            // Filter to only show upcoming events
            var upcomingRsvps = myRsvps
                .Where(r => r.Event.StartUtc >= DateTime.UtcNow)
                .ToList();

            return View(upcomingRsvps);
        }

        // GET: Profile/Notifications
        public async Task<IActionResult> Notifications()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var notifications = await _context.Notifications
                .Where(n => n.RecipientUserId == userId)
                .Include(n => n.Event)
                .Include(n => n.RecipientUser)
                .OrderByDescending(n => n.CreatedUtc)
                .Take(50) // Limit to last 50 notifications
                .ToListAsync();

            return View(notifications);
        }

        // POST: Profile/MarkNotificationRead/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == userId);

            if (notification == null)
            {
                return NotFound();
            }

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Notifications));
        }

        // POST: Profile/MarkAllNotificationsRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllNotificationsRead()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var notifications = await _context.Notifications
                .Where(n => n.RecipientUserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "All notifications marked as read.";
            return RedirectToAction(nameof(Notifications));
        }

        // POST: Profile/DeleteNotification/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == userId);

            if (notification == null)
            {
                return NotFound();
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Notification deleted.";
            return RedirectToAction(nameof(Notifications));
        }

        // GET: Profile/EventHistory
        public async Task<IActionResult> EventHistory()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            // Get past events user RSVP'd to
            var pastRsvps = await _context.RSVPs
                .Where(r => r.UserId == userId && r.Event.StartUtc < DateTime.UtcNow)
                .Include(r => r.Event)
                    .ThenInclude(e => e.CreatedBy)
                .OrderByDescending(r => r.Event.StartUtc)
                .ToListAsync();

            return View(pastRsvps);
        }

        // GET: Profile/Stats
        public async Task<IActionResult> Stats()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            // Events created
            var eventsCreated = await _context.Events
                .CountAsync(e => e.CreatedById == userId);

            // Total RSVPs received on user's events
            var totalRsvpsReceived = await _context.RSVPs
                .Where(r => r.Event.CreatedById == userId)
                .CountAsync();

            // Events attended (past RSVPs)
            var eventsAttended = await _context.RSVPs
                .Where(r => r.UserId == userId && r.Event.StartUtc < DateTime.UtcNow)
                .CountAsync();

            // Upcoming events (future RSVPs)
            var upcomingEvents = await _context.RSVPs
                .Where(r => r.UserId == userId && r.Event.StartUtc >= DateTime.UtcNow)
                .CountAsync();

            // Most popular category user attends
            var favoriteCategory = await _context.RSVPs
                .Where(r => r.UserId == userId)
                .GroupBy(r => r.Event.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .FirstOrDefaultAsync();

            ViewBag.EventsCreated = eventsCreated;
            ViewBag.TotalRsvpsReceived = totalRsvpsReceived;
            ViewBag.EventsAttended = eventsAttended;
            ViewBag.UpcomingEvents = upcomingEvents;
            ViewBag.FavoriteCategory = favoriteCategory?.Category.ToString() ?? "None";
            ViewBag.FavoriteCategoryCount = favoriteCategory?.Count ?? 0;

            return View();
        }
    }
}