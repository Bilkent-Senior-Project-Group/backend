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
    }
}
