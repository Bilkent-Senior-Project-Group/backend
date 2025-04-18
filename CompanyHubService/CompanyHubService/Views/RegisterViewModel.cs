using System.ComponentModel.DataAnnotations;

namespace CompanyHubService.Views
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "First name is required.")]
        public String FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        public String LastName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public String Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public String Password { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        public String Phone { get; set; }

        [Required(ErrorMessage = "Username is required.")]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Username can only contain letters and numbers.")]
        public String Username { get; set; }

        public Guid? CompanyId { get; set; } // for invite registration it is optional

    }
}
