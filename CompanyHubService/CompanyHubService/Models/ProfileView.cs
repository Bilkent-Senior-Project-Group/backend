using System;
using System.ComponentModel.DataAnnotations.Schema;
using CompanyHubService.Data;

namespace CompanyHubService.Models
{

    public class ProfileView
    {
        public string Id { get; set; }
        public string VisitorUserId { get; set; }
        public Guid CompanyId { get; set; } // Ziyaret edilen şirketin ID'si
        public DateTime ViewDate { get; set; }
        public int FromWhere{ get; set; } // 0: normal, 1: search query
    }
}