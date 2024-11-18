using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace CompanyHubService.Models
{
    public class User : IdentityUser
    {
        //The IdentityUser class already contains ID, UserName property and other properties.
        // public string UserId { get; set; } 
        public string FirstName { get; set; }
        public string LastName { get; set; }
        // public string UserName { get; set; } 
        // public string Password { get; set; }
        //public string Email { get; set; }
        //public string PhoneNumber { get; set; }
        public DateTime SignupDate { get; set; } = DateTime.UtcNow;
    }
}
