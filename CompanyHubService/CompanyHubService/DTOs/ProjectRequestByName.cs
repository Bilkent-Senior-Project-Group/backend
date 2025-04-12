using System.ComponentModel.DataAnnotations;
using CompanyHubService.Validation;
using CompanyHubService.Models;

namespace CompanyHubService.DTOs
{
    public class ProjectRequestByNameDTO
    {
        [Required]
        public string ClientCompanyName { get; set; }

        [Required]
        public string ProviderCompanyName { get; set; }

        [Required]
        public string ProjectName { get; set; }

        public string Description { get; set; }
        public List<string> TechnologiesUsed { get; set; }
        public string ClientType { get; set; }

        public List<Service> Services { get; set; }
    }
}



