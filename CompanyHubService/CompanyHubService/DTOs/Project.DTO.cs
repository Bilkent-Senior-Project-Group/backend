namespace CompanyHubService.DTOs
{
    public class ProjectDTO
    {
        public string ProjectName { get; set; }
        public string Description { get; set; }
        public List<string> TechnologiesUsed { get; set; } // List for easier input
        public string Industry { get; set; }
        public string ClientType { get; set; }
        public string Impact { get; set; }
        public string Date { get; set; }
        public string ProjectUrl { get; set; }
    }
}
