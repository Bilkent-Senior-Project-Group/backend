using System.Text.Json.Serialization;

namespace CompanyHubService.DTOs
{
    public class ProjectDTO
    {
        [JsonPropertyName("project_name")]  // Explicitly map JSON field
        public string ProjectName { get; set; }
        public string Description { get; set; }

        [JsonPropertyName("technologies_used")]  // Explicitly map JSON field
        public List<string> TechnologiesUsed { get; set; } // List for easier input
        public string Industry { get; set; }

        [JsonPropertyName("client_type")]  // Explicitly map JSON field
        public string ClientType { get; set; }
        public string Impact { get; set; }
        public string Date { get; set; }

        [JsonPropertyName("project_url")]  // Explicitly map JSON field
        public string ProjectUrl { get; set; }
    }
}
