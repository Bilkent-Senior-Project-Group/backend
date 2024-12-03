using System.ComponentModel.DataAnnotations;

namespace CompanyHubService.DTOs
{
    public class CreateCompanyRequestDTO
    {
        [Required]
        public string CompanyName { get; set; } // Company name is required

        [Required]
        [Range(1800, 2100)] // Foundation year must be within a realistic range
        public int FoundationYear { get; set; }

        [Required]
        public string Address { get; set; } // Address is required

    }

}

