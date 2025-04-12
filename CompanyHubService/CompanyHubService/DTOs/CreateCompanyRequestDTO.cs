using System.ComponentModel.DataAnnotations;
using CompanyHubService.Validation;

namespace CompanyHubService.DTOs
{
    // This is used when already existing user wants to add a company to the application
    public class CreateCompanyRequestDTO
    {
        [Required]
        public string CompanyName { get; set; } // Company name is required

        public string Description { get; set; }

        [Required]
        [Range(1800, 2100)] // Foundation year must be within a realistic range
        public int FoundedYear { get; set; }

        [Required]
        public string Address { get; set; } // Address is required
        public string Location { get; set; }
        public string Website { get; set; }
        public string CompanySize { get; set; }

        public string Phone { get; set; }

        public string Email { get; set; }

        public List<Guid> Services { get; set; }
        public List<string> Partnerships { get; set; } = new List<string>(); // Not required

        [Required]
        [EnsureAtLeastOneProject]
        public List<ProjectDTO>? Portfolio { get; set; } // List of projects

    }

}

