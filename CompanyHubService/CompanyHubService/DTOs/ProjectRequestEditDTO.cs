namespace CompanyHubService.DTOs
{
    public class ProjectRequestEditDTO
    {
        public string ProjectName { get; set; }
        public string Description { get; set; }
        public List<string> TechnologiesUsed { get; set; }
        public string ClientType { get; set; }
        public List<Guid> ServiceIds { get; set; }
    }

}