using System.ComponentModel.DataAnnotations;

namespace Townsquare.Models
{
    public class Event
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string Title { get; set; } = "";

        [Required, StringLength(4000)]
        public string Description { get; set; } = "";

        [Required]
        public DateTime StartUtc { get; set; }

        [Required, StringLength(160)]
        public string Location { get; set; } = "";

        [Required]
        public EventCategory Category { get; set; }

        public string? CreatedById { get; set; }
        public User? CreatedBy { get; set; }

        public ICollection<RSVP> RSVPs { get; set; } = new List<RSVP>();
    }
}

