using System.ComponentModel.DataAnnotations;

namespace Townsquare.Models
{
    public class RSVP
    {
        public int Id { get; set; }

        [Required]
        public int EventId { get; set; }
        public Event Event { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = null!;
        public User User { get; set; } = null!;
        public bool IsGoing { get; set; } = true;

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}

