namespace CompanyHubService.DTOs
{
    public class CompanyServiceMatchDTO : CompanyProfileDTO
    {
        public int MatchingServiceCount { get; set; }
        public int TotalServiceCount { get; set; }
    }
}
