using System.ComponentModel.DataAnnotations;

namespace CompanyHubService.Models
{
    public class CitiesAndCountries
    {
        [Key]
        public int ID { get; set; }

        public string? City { get; set; }

        public string? Country { get; set; }
    }
}
