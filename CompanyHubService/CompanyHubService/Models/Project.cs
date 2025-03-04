using System;

namespace CompanyHubService.Models
{
    public class Project
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Description { get; set; }
        public string TechnologiesUsed { get; set; }
        public string Industry { get; set; }
        public string ClientType { get; set; }
        public string Impact { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime CompletionDate { get; set; }
        public bool IsOnCompedia { get; set; }
        public bool IsCompleted { get; set; } = false;
        public string? ProjectUrl { get; set; }

        // Nav prop
        public ProjectCompany ProjectCompany { get; set; }
    }
}
