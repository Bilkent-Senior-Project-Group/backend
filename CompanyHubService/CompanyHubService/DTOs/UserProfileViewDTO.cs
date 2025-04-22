namespace CompanyHubService.DTOs
{
    public class UserProfileDTO
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhotoUrl { get; set; }
        public string PhoneNumber { get; set; }
        public string UserName { get; set; }

        public string Email { get; set; }

        public string? Bio { get; set; }

        public string? Position { get; set; }

        public string? LinkedInUrl { get; set; }
    }

}