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
        public int FoundationYear { get; set; }
        public int CompanySize { get; set; }

        [Required]
        public string Address { get; set; }
        public DateTime SignupDate { get; set; }

        // Navigation property for Products
        public ICollection<Product> Products { get; set; } // One-to-Many relationship

        public ICollection<ProductClient> ClientProductClients { get; set; } // Products this company is a client of
    }
}