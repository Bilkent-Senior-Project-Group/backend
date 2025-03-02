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
        public int CompanySize { get; set; }
        public string Address { get; set; }
        public string Specialties { get; set; }
        public string Industries { get; set; }
        //public DateTime SignupDate { get; set; }
        public string CoreExpertise { get; set; } // comma-separated string

        public string Location { get; set; }
        public string Website { get; set; }
        public bool Verified { get; set; }
        public string ContactInfo { get; set; }
        public List<Project> Projects { get; set; } = new List<Project>();
        // Navigation property for Products
        public ICollection<Product> Products { get; set; } // One-to-Many relationship

        public ICollection<ProductClient> ClientProductClients { get; set; } // Products this company is a client of
    }
}