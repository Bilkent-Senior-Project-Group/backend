using System;

namespace CompanyHubService.Models
{
    public class Project
    {
        public Guid ProjectId { get; set; }
        public Guid CompanyId { get; set; }
        public string ProjectName { get; set; }
        public string Description { get; set; }
        public string TechnologiesUsed { get; set; }
        public string Industry { get; set; }
        public string ClientType { get; set; }
        public string Impact { get; set; }
        public string Date { get; set; }
        public string ProjectUrl { get; set; }

        // Nav prop
        public Company Company { get; set; }
    }
}
