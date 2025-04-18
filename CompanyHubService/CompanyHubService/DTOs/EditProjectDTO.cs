namespace CompanyHubService.DTOs
{
    public class EditProjectDTO
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Description { get; set; }
        public string TechnologiesUsed { get; set; }
        public string ClientType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime CompletionDate { get; set; }
        public string ProjectUrl { get; set; }
        public List<Guid> Services { get; set; }
    }

}