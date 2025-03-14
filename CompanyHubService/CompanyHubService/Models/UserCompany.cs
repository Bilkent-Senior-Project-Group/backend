using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace CompanyHubService.Models
{
    public class UserCompany
    {
        [Key]
        public int UserCompanyID { get; set; }

        [ForeignKey("User")]
        public string UserId { get; set; }

        [ForeignKey("Company")]
        public Guid CompanyId { get; set; }

        public User? User { get; set; }

        public Company? Company { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.Now;


    }
}
