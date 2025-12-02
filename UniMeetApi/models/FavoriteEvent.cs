using System.ComponentModel.DataAnnotations;

namespace UniMeetApi
{
    public class FavoriteEvent
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int EventId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation props (optional)
        public User? User { get; set; }
        public Event? Event { get; set; }
    }
}
