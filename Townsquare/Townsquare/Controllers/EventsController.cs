using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Townsquare.Data;
using Townsquare.Models;
using Townsquare.Services;

namespace Townsquare.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWeatherService _weatherService;
        private readonly UserManager<User> _userManager;

        public EventsController(ApplicationDbContext context, IWeatherService weatherService, UserManager<User> userManager)
        {
            _context = context;
            _weatherService = weatherService;
            _userManager = userManager;
        }

        // GET: Events
        public async Task<IActionResult> Index(string searchString, EventCategory? category, DateTime? startDate, DateTime? endDate)
        {
            var events = _context.Events
                .Include(e => e.CreatedBy)
                .Include(e => e.RSVPs)
                .AsQueryable();

            // Search by keywords (title, description, location)
            if (!string.IsNullOrEmpty(searchString))
            {
                events = events.Where(e => 
                    e.Title.Contains(searchString) || 
                    e.Description.Contains(searchString) || 
                    e.Location.Contains(searchString));
            }

            // Filter by category
            if (category.HasValue)
            {
                events = events.Where(e => e.Category == category.Value);
            }

            // Filter by date range
            if (startDate.HasValue)
            {
                events = events.Where(e => e.StartUtc >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                events = events.Where(e => e.StartUtc <= endDate.Value);
            }

            // Order by start date
            events = events.OrderBy(e => e.StartUtc);

            // Pass search parameters to view
            ViewBag.SearchString = searchString;
            ViewBag.SelectedCategory = category;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            return View(await events.ToListAsync());
        }

        // GET: Events/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events
                .Include(e => e.CreatedBy)
                .Include(e => e.RSVPs)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (@event == null)
            {
                return NotFound();
            }

            // Check if current user has RSVP'd
            var currentUserId = _userManager.GetUserId(User);
            var userHasRsvp = false;
            if (currentUserId != null)
            {
                userHasRsvp = @event.RSVPs.Any(r => r.UserId == currentUserId);
            }

            ViewBag.UserHasRsvp = userHasRsvp;
            ViewBag.IsLoggedIn = User.Identity?.IsAuthenticated ?? false;
            ViewBag.CurrentUserId = currentUserId;
            ViewBag.IsEventCreator = @event.CreatedById == currentUserId;

            // Hämta väder från extern API
            try
            {
                var weather = await _weatherService.GetWeatherAsync(@event.Location);
                ViewBag.Weather = weather;
            }
            catch (Exception ex)
            {
                // Log error but don't fail the request
                Console.WriteLine($"Error loading weather: {ex.Message}");
                ViewBag.Weather = null;
            }

            return View(@event);
        }

        // GET: Events/Create
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Events/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,StartUtc,Location,Category")] Event @event)
        {
            if (ModelState.IsValid)
            {
                // Set the creator to the current user
                @event.CreatedById = _userManager.GetUserId(User);
                if (@event.CreatedById == null)
                {
                    return Unauthorized();
                }

                _context.Add(@event);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(@event);
        }

        // GET: Events/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events.FindAsync(id);
            if (@event == null)
            {
                return NotFound();
            }

            // Check if user is authorized to edit this event
            var currentUserId = _userManager.GetUserId(User);
            if (@event.CreatedById != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            return View(@event);
        }

        // POST: Events/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,StartUtc,Location,Category,CreatedById")] Event @event)
        {
            if (id != @event.Id)
            {
                return NotFound();
            }

            // Check if user is authorized to edit this event
            var existingEvent = await _context.Events.FindAsync(id);
            if (existingEvent == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            if (existingEvent.CreatedById != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update the existing tracked entity instead of using Update()
                    existingEvent.Title = @event.Title;
                    existingEvent.Description = @event.Description;
                    existingEvent.StartUtc = @event.StartUtc;
                    existingEvent.Location = @event.Location;
                    existingEvent.Category = @event.Category;
                    // CreatedById remains unchanged

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventExists(@event.Id))
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
            return View(@event);
        }

        // GET: Events/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events
                .Include(e => e.CreatedBy)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (@event == null)
            {
                return NotFound();
            }

            // Check if user is authorized to delete this event
            var currentUserId = _userManager.GetUserId(User);
            if (@event.CreatedById != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            return View(@event);
        }

        // POST: Events/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var @event = await _context.Events.FindAsync(id);
            if (@event != null)
            {
                // Check if user is authorized to delete this event
                var currentUserId = _userManager.GetUserId(User);
                if (@event.CreatedById != currentUserId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                _context.Events.Remove(@event);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.Id == id);
        }

    }
}
