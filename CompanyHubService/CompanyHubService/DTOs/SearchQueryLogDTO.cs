namespace CompanyHubService.DTOs
{
    public class SearchQueryLogDTO {
        public int Id { get; set; }

        public string VisitorId { get; set; }
        public List<Guid> CompanyIds { get; set; } = new List<Guid>();

        public string QueryText { get; set; }
        public DateTime SearchDate { get; set; }
    }
}