using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniMeetApi
{
    public enum NotificationType
    {
        EventCreated,
        EventUpdated,
        EventCancelled,
        ClubAnnouncement
    }

    public enum NotificationStatus
    {
        Pending,
        Sent,
        Failed,
        Retry
    }

    public class NotificationLog
    {
        [Key]
        public int NotificationLogId { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        public int? EventId { get; set; }

        [ForeignKey("EventId")]
        public Event? Event { get; set; }

        public int? ClubId { get; set; }

        [ForeignKey("ClubId")]
        public Club? Club { get; set; }

        [Required]
        public NotificationType Type { get; set; }

        [Required]
        public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

        [Required, EmailAddress]
        public string RecipientEmail { get; set; } = null!;

        [Required]
        public string Subject { get; set; } = null!;

        [Required]
        public string Body { get; set; } = null!;

        public string? ErrorMessage { get; set; }

        public int RetryCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? SentAt { get; set; }
    }
}
