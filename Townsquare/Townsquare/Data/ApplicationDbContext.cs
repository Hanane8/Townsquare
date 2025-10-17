using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Townsquare.Models;

namespace Townsquare.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
             : base(options) { }

        // DbSets
        public DbSet<Event> Events => Set<Event>();
        public DbSet<RSVP> RSVPs => Set<RSVP>();
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // Event -> CreatedBy (nullable: event överlever om skaparen tas bort)
            b.Entity<Event>()
             .HasOne(e => e.CreatedBy)
             .WithMany()
             .HasForeignKey(e => e.CreatedById)
             .OnDelete(DeleteBehavior.SetNull);

            // Unik RSVP per (Event, User)
            b.Entity<RSVP>()
             .HasIndex(x => new { x.EventId, x.UserId })
             .IsUnique();

            b.Entity<RSVP>()
             .HasOne(x => x.Event)
             .WithMany(e => e.RSVPs)
             .HasForeignKey(x => x.EventId)
             .OnDelete(DeleteBehavior.Cascade);

            b.Entity<RSVP>()
             .HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            b.Entity<Notification>()
             .HasOne(n => n.RecipientUser)
             .WithMany()
             .HasForeignKey(n => n.RecipientUserId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

