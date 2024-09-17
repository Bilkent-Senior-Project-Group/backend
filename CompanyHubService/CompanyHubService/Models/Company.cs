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
    }
}