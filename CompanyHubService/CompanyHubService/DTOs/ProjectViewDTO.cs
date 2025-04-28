using CompanyHubService.Models;

namespace CompanyHubService.DTOs
{
    public class ProjectViewDTO
    {
        public Guid ProjectId { get; set; }

        //[JsonPropertyName("project_name")]  // Explicitly map JSON field
        public string ProjectName { get; set; }
        public string Description { get; set; }

        //[JsonPropertyName("technologies_used")]  // Explicitly map JSON field
        public List<string> TechnologiesUsed { get; set; } // List for easier input

        //[JsonPropertyName("client_type")]  // Explicitly map JSON field
        public string ClientType { get; set; }
        public DateTime StartDate { get; set; }

        public DateTime? CompletionDate { get; set; }

        public bool IsOnCompedia { get; set; }

        public bool IsCompleted { get; set; }

        public string ClientCompanyName { get; set; }
        public string ProviderCompanyName { get; set; }

        //[JsonPropertyName("project_url")]  // Explicitly map JSON field
        public string ProjectUrl { get; set; }

        public List<ServiceDTO> Services { get; set; } // Change this to ServiceDTO
    }
}
