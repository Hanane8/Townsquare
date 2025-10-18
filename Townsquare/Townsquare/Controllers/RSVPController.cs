using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Townsquare.Data;
using Townsquare.Models;

namespace Townsquare.Controllers
{
    [Authorize]
    public class RsvpController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public RsvpController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // POST: Rsvp/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int eventId)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            // Check if event exists
            var eventExists = await _context.Events.AnyAsync(e => e.Id == eventId);
            if (!eventExists)
            {
                return NotFound();
            }

            // Check if user already RSVP'd
            var existingRsvp = await _context.RSVPs
                .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);

            if (existingRsvp != null)
            {
                TempData["Warning"] = "You have already RSVP'd to this event.";
                return RedirectToAction("Details", "Events", new { id = eventId });
            }

            // Create new RSVP
            var rsvp = new RSVP
            {
                EventId = eventId,
                UserId = userId,
                CreatedUtc = DateTime.UtcNow
            };

            _context.RSVPs.Add(rsvp);

            // Create notification for event creator
            var eventItem = await _context.Events
                .Include(e => e.CreatedBy)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventItem?.CreatedById != null && eventItem.CreatedById != userId)
            {
                var user = await _userManager.GetUserAsync(User);
                var notification = new Notification
                {
                    RecipientUserId = eventItem.CreatedById,
                    Message = $"{user?.FullName ?? "Someone"} has RSVP'd to your event '{eventItem.Title}'",
                    EventId = eventId,
                    CreatedUtc = DateTime.UtcNow,
                    IsRead = false
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "You have successfully RSVP'd to this event!";
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        // POST: Rsvp/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int eventId)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var rsvp = await _context.RSVPs
                .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);

            if (rsvp == null)
            {
                return NotFound();
            }

            _context.RSVPs.Remove(rsvp);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your RSVP has been cancelled.";
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        // GET: Rsvp/GetAttendees/5
        [HttpGet]
        public async Task<IActionResult> GetAttendees(int eventId)
        {
            var eventExists = await _context.Events.AnyAsync(e => e.Id == eventId);
            if (!eventExists)
            {
                return NotFound();
            }

            var attendees = await _context.RSVPs
                .Where(r => r.EventId == eventId)
                .Include(r => r.User)
                .Select(r => new
                {
                    r.User.FullName,
                    r.CreatedUtc
                })
                .OrderBy(a => a.CreatedUtc)
                .ToListAsync();

            return Json(attendees);
        }

        // GET: Rsvp/Count/5
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Count(int eventId)
        {
            var count = await _context.RSVPs
                .CountAsync(r => r.EventId == eventId);

            return Json(new { count });
        }

        // GET: Rsvp/HasUserRsvp/5
        [HttpGet]
        public async Task<IActionResult> HasUserRsvp(int eventId)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Json(new { hasRsvp = false });
            }

            var hasRsvp = await _context.RSVPs
                .AnyAsync(r => r.EventId == eventId && r.UserId == userId);

            return Json(new { hasRsvp });
        }
    }
}