namespace CompanyHubService.DTOs
{
    public class FreeTextSearchDTO
    {
        public string searchQuery { get; set; }
        public List<int>? locations { get; set; }
        public List<Guid>? serviceIds { get; set; }
    }
}
