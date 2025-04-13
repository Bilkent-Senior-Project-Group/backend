namespace CompanyHubService.Models
{
    public class Service
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public Guid IndustryId { get; set; }

        public Industry Industry { get; set; }

        public ICollection<ServiceProject> ServiceProjects { get; set; }
    }
}
