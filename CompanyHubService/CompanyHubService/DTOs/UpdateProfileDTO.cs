namespace CompanyHubService.DTOs
{
    public class UpdateProfileDTO
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }

        public string? Bio { get; set; }

        public string? Position { get; set; }

        public string? LinkedInUrl { get; set; }
    }
}