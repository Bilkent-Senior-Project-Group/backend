using CompanyHubService.Models;
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
        public int Verified { get; set; }

        // [Required]
        // [JsonPropertyName("portfolio")]  // Map JSON "portfolio" to "Projects"
        public List<ProjectViewDTO> Projects { get; set; } = new List<ProjectViewDTO>();

        // [Required]
        public int Location { get; set; }

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

        public List<ServiceCompany> Services { get; set; }
    }
}
