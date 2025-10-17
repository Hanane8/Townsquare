using System.ComponentModel.DataAnnotations;

namespace Townsquare.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string RecipientUserId { get; set; } = null!;
        public User RecipientUser { get; set; } = null!;

        [Required]
        public int EventId { get; set; }
        public Event Event { get; set; } = null!;

        [Required, StringLength(240)]
        public string Message { get; set; } = "";

        public bool IsRead { get; set; } = false;
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}

