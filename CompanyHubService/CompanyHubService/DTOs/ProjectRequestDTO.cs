using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CompanyHubService.DTOs
{
    public class ProjectRequestDTO
    {
        [Required]
        public Guid ClientCompanyId { get; set; } // ✅ The company making the request

        [Required]
        public Guid ProviderCompanyId { get; set; } // ✅ The company providing the service

        [Required]
        public string ProjectName { get; set; }

        public string Description { get; set; }

        public List<string> TechnologiesUsed { get; set; } // List for easier input

        public string Industry { get; set; }

        public string ClientType { get; set; }

        public string Impact { get; set; }
    }
}
