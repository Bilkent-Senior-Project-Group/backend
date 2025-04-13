using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompanyHubService.Models
{
    public class ProjectCompany
    {
        [Key]
        public Guid ProjectId { get; set; }

        [Required]
        public Guid? ClientCompanyId { get; set; }


        public Guid? ProviderCompanyId { get; set; }

        [ForeignKey("ProjectId")]
        public Project Project { get; set; }

        [ForeignKey("ClientCompanyId")]
        public Company ClientCompany { get; set; }

        [ForeignKey("ProviderCompanyId")]
        public Company ProviderCompany { get; set; }

        public string? OtherCompanyName { get; set; } // Used when the company is not in the database

        public int IsClient { get; set; } // Indicates if the company is a client or provider
    }
}
