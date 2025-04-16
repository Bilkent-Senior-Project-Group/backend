
namespace CompanyHubService.DTOs
{
   public class ProfileViewDTO
    {
        public int Id { get; set; } // Non-nullable int, database-generated
        public string VisitorUserId { get; set; }
        public Guid CompanyId { get; set; }
        public DateTime ViewDate { get; set; }
        public int FromWhere { get; set; } // 0: normal, 1: search query
    }

}