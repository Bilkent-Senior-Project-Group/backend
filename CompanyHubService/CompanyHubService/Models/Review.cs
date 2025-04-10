using System.ComponentModel.DataAnnotations.Schema;

namespace CompanyHubService.Models
{
    // Review on a company
    public class Review
    {
        public Guid ReviewId { get; set; } = Guid.NewGuid();
        public string ReviewText { get; set; }
        public int Rating { get; set; }
        public DateTime DatePosted { get; set; }

        // Nav prop
        public Guid ProjectId { get; set; }
        public Project Project { get; set; }

        public string UserId { get; set; }
        public User User { get; set; }
    }
}