using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompanyHubService.Models
{
    public class ServiceCompany
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]
        public int Id { get; set; }
        public Guid CompanyId { get; set; }

        public Guid ServiceId { get; set; }

        public int Percentage { get; set; }


        [ForeignKey("CompanyId")]
        public Company Company { get; set; }

        [ForeignKey("ServiceId")]

        public Service Service { get; set; }
    }
}
