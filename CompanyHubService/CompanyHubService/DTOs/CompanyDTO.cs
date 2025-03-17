namespace CompanyHubService.DTOs
{
    // Might need to MAP this one too.
    public class CompanyDTO
    {
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;

        // List of projects under the company
        public List<ProjectDTO> Projects { get; set; } = new List<ProjectDTO>();
    }
}