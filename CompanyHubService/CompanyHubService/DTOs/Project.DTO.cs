using System.Text.Json.Serialization;
using CompanyHubService.Models;

namespace CompanyHubService.DTOs
{
    public class ProjectDTO
    {
        public Guid ProjectId { get; set; }

        [JsonPropertyName("project_name")]  // Explicitly map JSON field
        public string ProjectName { get; set; }
        public string Description { get; set; }

        [JsonPropertyName("technologies_used")]  // Explicitly map JSON field
        public List<string> TechnologiesUsed { get; set; } // List for easier input
        public string Industry { get; set; }

        [JsonPropertyName("client_type")]  // Explicitly map JSON field
        public string ClientType { get; set; }
        public string Impact { get; set; }
        public DateTime StartDate { get; set; }

        public DateTime CompletionDate { get; set; }

        public bool IsOnCompedia { get; set; }

        public bool IsCompleted { get; set; }

        public CompanyDTO ClientCompany { get; set; }
        public CompanyDTO ProviderCompany { get; set; }

        [JsonPropertyName("project_url")]  // Explicitly map JSON field
        public string ProjectUrl { get; set; }

        public CompanyDTO Company { get; set; }
    }
}
