namespace CompanyHubService.DTOs
{
    public class UserCompanyWithTechnologiesDTO
    {
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; }
        public List<string> Services { get; set; }
        public List<string> TechnologiesUsed { get; set; }
    }

}