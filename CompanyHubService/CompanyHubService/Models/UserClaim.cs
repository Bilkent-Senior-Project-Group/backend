using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
namespace CompanyHubService.Models

{
    public class UserClaim : IdentityUserClaim<string>
    {
        [ForeignKey("Company")]
        public Guid? CompanyId { get; set; } // Foreign key to Companies table
        public Company? Company { get; set; }
    }
}