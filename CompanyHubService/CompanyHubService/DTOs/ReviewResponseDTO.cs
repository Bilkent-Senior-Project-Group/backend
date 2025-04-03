namespace CompanyHubService.DTOs
{
    public class ReviewResponseDTO
    {
        public Guid ReviewId { get; set; }
        public string ReviewText { get; set; }
        public int Rating { get; set; }
        public DateTime DatePosted { get; set; }

        public string CompanyName { get; set; }
        public string UserName { get; set; }
    }

}