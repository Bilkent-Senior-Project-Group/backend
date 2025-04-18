using CompanyHubService.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization; // Required for JSON mapping

namespace CompanyHubService.DTOs
{
    public class CompanyProfileModifyDTO
    {
        public Guid CompanyId { get; set; } = Guid.NewGuid();

        // [Required]
        public string Name { get; set; }

        public string Description { get; set; }
        // [Required]
        public int Location { get; set; }

        // [Required]
        public string Website { get; set; }

        public List<string> Partnerships { get; set; } = new List<string>(); // Not required

        // [Required]
        public string CompanySize { get; set; }

        // [Required]
        public int FoundedYear { get; set; }

        // [Required]
        // [JsonPropertyName("contact_info")]  // Ensure correct mapping
        public string Phone { get; set; }

        public string Email { get; set; }

        // [Required]
        public string Address { get; set; }

        public List<ServiceModifyDTO> Services { get; set; }
    }
}
