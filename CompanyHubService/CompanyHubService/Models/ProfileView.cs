using System;
using System.ComponentModel.DataAnnotations.Schema;
using CompanyHubService.Data;

namespace CompanyHubService.Models
{

    public class ProfileView
    {
        public int Id { get; set; }
        public string VisitorUserId { get; set; }  // Changed from Guid to string to match User.Id type
        public Guid CompanyId { get; set; }
        public DateTime ViewDate { get; set; }
        public int FromWhere { get; set; }  // 0: normal, 1: search query
    }

}