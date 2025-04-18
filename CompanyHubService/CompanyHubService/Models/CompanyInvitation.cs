using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompanyHubService.Models
{
    public class CompanyInvitation
    {
        [Key]
        public Guid InvitationId { get; set; } = Guid.NewGuid();
        public Guid CompanyId { get; set; }
        public string UserId { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool Accepted { get; set; } = false;
        public bool Rejected { get; set; } = false;

        [ForeignKey("CompanyId")]
        public Company Company { get; set; }
    }

}