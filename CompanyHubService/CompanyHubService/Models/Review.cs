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
        public Guid CompanyId { get; set; }
        public Company Company { get; set; }

        public string UserId { get; set; }
        public User User { get; set; }
    }
}