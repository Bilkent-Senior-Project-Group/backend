using CompanyHubService.Models;

namespace CompanyHubService.DTOs
{
    public class CompanyKafkaDTO
    {
        public Guid CompanyId { get; set; }
        public string Description { get; set; }
        public int FoundedYear { get; set; }
        public string CompanySize { get; set; }
        public int Location { get; set; }
        public double OverallRating { get; set; }
        public ICollection<ServiceCompany> ServiceCompanies { get; set; }
        public List<Project> Projects { get; set; }
        public ICollection<Product> Products { get; set; }
        public ICollection<Review> Reviews { get; set; }
    }
}
