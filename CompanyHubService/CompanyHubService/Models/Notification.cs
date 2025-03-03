using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CompanyHubService.Models
{
    public class Notification
    {
        [Key]
        public Guid NotificationId { get; set; } = Guid.NewGuid();

        [Required]
        public string RecipientId { get; set; }  // User ID

        [Required]
        public string Message { get; set; }

        [Required]
        public string NotificationType { get; set; } // This can be User-Related and Company-Related (maybe rather than string use enum??)

        public string Url { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReadAt { get; set; }
    }
}


