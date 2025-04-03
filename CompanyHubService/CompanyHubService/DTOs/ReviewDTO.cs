using System.ComponentModel.DataAnnotations;

namespace CompanyHubService.DTOs
{
    public class ReviewDTO
    {
        public string ReviewText { get; set; }

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        public Guid CompanyId { get; set; } // ✅ Company being reviewed
    }
}