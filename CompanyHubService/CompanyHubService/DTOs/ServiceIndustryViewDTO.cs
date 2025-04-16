namespace CompanyHubService.DTOs
{
    public class ServiceIndustryViewDTO
    {
        public Guid Id { get; set; }              // ServiceId
        public string? ServiceName { get; set; }          // ServiceName
        public Guid IndustryId { get; set; }
        public string? IndustryName { get; set; }
        public int Percentage { get; set; }       // From ServiceCompany
    }
}