using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CompanyHubService.Models;

namespace CompanyHubService.DTOs
{
    public class ProjectRequestViewDTO
    {
        public Guid RequestId { get; set; }

        [Required]
        public Guid ClientCompanyId { get; set; } // ✅ The company making the request

        [Required]
        public Guid ProviderCompanyId { get; set; } // ✅ The company providing the service

        [Required]
        public string ProjectName { get; set; }

        public string ClientCompanyName { get; set; } // ✅ The name of the company making the request
        public string ProviderCompanyName { get; set; } // ✅ The name of the company providing the service

        public string Description { get; set; }

        public List<string> TechnologiesUsed { get; set; } // List for easier input

        public string ClientType { get; set; }

        public List<ServiceDTO> Services { get; set; }

    }
}
