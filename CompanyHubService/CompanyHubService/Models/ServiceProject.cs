using CompanyHubService.DTOs;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompanyHubService.Models
{
    public class ServiceProject
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]
        public int Id { get; set; }
        public Guid ProjectId { get; set; }

        public Guid ServiceId { get; set; }


        [ForeignKey("ProjectId")]
        public Project Project { get; set; }

        [ForeignKey("ServiceId")]

        public Service Service { get; set; }
    }
}
