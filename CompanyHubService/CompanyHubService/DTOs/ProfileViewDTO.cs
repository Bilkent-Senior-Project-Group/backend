
namespace CompanyHubService.DTOs
{
    public class ProfileViewDTO
    {
        public string Id { get; set; }
        public string VisitorUserId { get; set; }
        public Guid CompanyId { get; set; } // Ziyaret edilen şirketin sayfa ID'si
        public DateTime ViewDate { get; set; }
        public int FromWhere{ get; set; } // 0: normal, 1: search query
    }
}