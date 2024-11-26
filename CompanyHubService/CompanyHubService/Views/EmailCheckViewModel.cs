using System.ComponentModel.DataAnnotations;

public class EmailCheckViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}
