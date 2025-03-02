namespace CompanyHubService.DTOs
{
    public class UserDTO
    {
        public string Id { get; set; } // Unique identifier
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

        // ✅ List of companies the user belongs to
        public List<CompanyDTO> Companies { get; set; } = new List<CompanyDTO>();

        // ✅ List of projects the user is associated with
        public List<ProjectDTO> Projects { get; set; } = new List<ProjectDTO>();

    }
}
