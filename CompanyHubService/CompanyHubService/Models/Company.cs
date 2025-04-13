using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CompanyHubService.Models
{
    public class Company
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]
        public Guid CompanyId { get; set; } = Guid.NewGuid();

        [Required]
        public string CompanyName { get; set; }

        public string Description { get; set; }
        public int FoundedYear { get; set; }
        public string CompanySize { get; set; }
        public string Address { get; set; }

        //public DateTime SignupDate { get; set; }
        public int Location { get; set; }
        public string Website { get; set; }
        public bool Verified { get; set; }

        public string Phone { get; set; }

        public string Email { get; set; }

        public double OverallRating { get; set; } = 0;

        public string LogoUrl { get; set; }

        public ICollection<ServiceCompany> ServiceCompanies { get; set; } = new List<ServiceCompany>();
        public List<Project> Projects { get; set; } = new List<Project>();
        // Navigation property for Products
        public ICollection<Product> Products { get; set; } // One-to-Many relationship DO NOT NEED THAT

        public ICollection<ProductClient> ClientProductClients { get; set; } // Products this company is a client of DO NOT NEED THAT
        public ICollection<Review> Reviews { get; set; } = new List<Review>(); // DO NOT NEED THAT
    }
}