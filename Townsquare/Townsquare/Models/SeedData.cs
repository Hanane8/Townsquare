using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Townsquare.Data;
using System;
using System.Linq;

namespace Townsquare.Models;

public static class SeedData
{
    public static void Initialize(IServiceProvider serviceProvider)
    {
        using (var context = new ApplicationDbContext(
            serviceProvider.GetRequiredService<
                DbContextOptions<ApplicationDbContext>>()))
        {
            // Look for any events.
            if (context.Events.Any())
            {
                return;   // DB has been seeded
            }

            context.Events.AddRange(
                new Event
                {
                    Title = "Jazz Concert in the Park",
                    Description = "Enjoy an evening of smooth jazz with local musicians. Bring your blankets and picnic baskets for a relaxing night under the stars.",
                    StartUtc = DateTime.UtcNow.AddDays(7),
                    Location = "Central Park, Borås",
                    Category = EventCategory.Concert
                },
                new Event
                {
                    Title = "Farmers Market",
                    Description = "Fresh local produce, artisan crafts, and homemade goods. Support local farmers and craftspeople while enjoying the community atmosphere.",
                    StartUtc = DateTime.UtcNow.AddDays(3),
                    Location = "Town Square, Borås",
                    Category = EventCategory.Market
                },
                new Event
                {
                    Title = "Coding Workshop: ASP.NET Core",
                    Description = "Learn the fundamentals of web development with ASP.NET Core MVC. Suitable for beginners with basic C# knowledge. Laptops required.",
                    StartUtc = DateTime.UtcNow.AddDays(14),
                    Location = "Tech Hub, Allégatan 1, Borås",
                    Category = EventCategory.Workshop
                },
                new Event
                {
                    Title = "Charity Run for Children",
                    Description = "5K and 10K runs to raise funds for local children's charities. All fitness levels welcome. Registration includes a t-shirt and refreshments.",
                    StartUtc = DateTime.UtcNow.AddDays(21),
                    Location = "Borås Stadium",
                    Category = EventCategory.Sports
                },
                new Event
                {
                    Title = "Summer Food Festival",
                    Description = "Taste dishes from around the world prepared by local restaurants and food trucks. Live music and family activities throughout the day.",
                    StartUtc = DateTime.UtcNow.AddDays(30),
                    Location = "Stadsparken, Borås",
                    Category = EventCategory.Other
                },
                new Event
                {
                    Title = "Photography Exhibition Opening",
                    Description = "Opening night of 'Urban Perspectives' - a collection of street photography from Borås and surrounding areas. Meet the artists and enjoy complimentary refreshments.",
                    StartUtc = DateTime.UtcNow.AddDays(10),
                    Location = "Borås Art Museum",
                    Category = EventCategory.Other
                }
            );

            context.SaveChanges();
        }
    }
}