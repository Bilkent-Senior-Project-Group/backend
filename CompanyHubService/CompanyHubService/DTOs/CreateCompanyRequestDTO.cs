using System.ComponentModel.DataAnnotations;

namespace CompanyHubService.DTOs
{
    // This is used when already existing user wants to add a company to the application
    public class CreateCompanyRequestDTO
    {
        [Required]
        public string CompanyName { get; set; } // Company name is required

        [Required]
        [Range(1800, 2100)] // Foundation year must be within a realistic range
        public int FoundedYear { get; set; }

        [Required]
        public string Address { get; set; } // Address is required

        public string Specialties { get; set; }
        public string Industries { get; set; }
        public string Location { get; set; }
        public string Website { get; set; }
        public int CompanySize { get; set; }
        public string ContactInfo { get; set; }
        public List<ProjectDTO> Portfolio { get; set; } // List of projects

    }

}

