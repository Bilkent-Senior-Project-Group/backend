using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CompanyHubService.DTOs;
using CompanyHubService.Models;

namespace CompanyHubService.Data
{
    // Maybe if rejected we can delete the request forever???
    public class ProjectRequest
    {
        [Key]
        public Guid RequestId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ClientCompanyId { get; set; }

        public Guid? ProviderCompanyId { get; set; }

        [Required]
        public string ProjectName { get; set; }

        public string Description { get; set; }

        public string TechnologiesUsed { get; set; }

        public string ClientType { get; set; }

        public string Impact { get; set; }

        public bool IsAccepted { get; set; } = false;

        public bool IsRejected { get; set; } = false;

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        public DateTime? AcceptedDate { get; set; }

        public List<Guid> Services { get; set; } // SIKINTI BURADA --> CompanyHubDBContext.cs'de comma seperated string olarak tanımlandı.

        [ForeignKey("ClientCompanyId")]
        public Company ClientCompany { get; set; }

        [ForeignKey("ProviderCompanyId")]
        public Company ProviderCompany { get; set; }
    }
}

