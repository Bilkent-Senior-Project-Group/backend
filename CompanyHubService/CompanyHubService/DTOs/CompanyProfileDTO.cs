using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization; // Required for JSON mapping

namespace CompanyHubService.DTOs
{
    public class CompanyProfileDTO
    {
        public Guid CompanyId { get; set; } = Guid.NewGuid();

        // [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        // [Required]
        public string Specialties { get; set; }

        // [Required]
        // [JsonPropertyName("core_expertise")]  // Explicitly map JSON field
        public List<string> CoreExpertise { get; set; } = new List<string>();

        // [Required]
        public int Verified { get; set; }

        // [Required]
        // [JsonPropertyName("portfolio")]  // Map JSON "portfolio" to "Projects"
        public List<ProjectDTO> Projects { get; set; } = new List<ProjectDTO>();

        // [Required]
        public List<string> Industries { get; set; } = new List<string>();

        // [Required]
        public string Location { get; set; }

        // [Required]
        public string Website { get; set; }

        public List<string> Partnerships { get; set; } = new List<string>(); // Not required

        // [Required]
        public string CompanySize { get; set; }

        // [Required]
        public int FoundedYear { get; set; }

        public double OverallRating { get; set; } = 0; // Default to 0

        // [Required]
        // [JsonPropertyName("contact_info")]  // Ensure correct mapping
        public string Phone { get; set; } = "Unknown"; // Default to "Unknown"

        public string Email { get; set; } = "Unknown"; // Default to "Unknown"

        // [Required]
        public string Address { get; set; } = "Unknown"; // Default to "Unknown"
    }
}
