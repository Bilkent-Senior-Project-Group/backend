namespace CompanyHubService.DTOs
{
    public class CompanyProfileDTO
    {
        public Guid CompanyId { get; set; }
        public string Name { get; set; }
        public string Specialties { get; set; }
        public List<string> CoreExpertise { get; set; }
        public int Verified { get; set; }
        public List<ProjectDTO> Projects { get; set; }
        public List<string> Industries { get; set; }
        public string Location { get; set; }
        public string Website { get; set; }
        public int CompanySize { get; set; }
        public int FoundedYear { get; set; }
        public string ContactInfo { get; set; }

        public string Address { get; set; }

    }
}
