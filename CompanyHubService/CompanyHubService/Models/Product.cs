namespace CompanyHubService.Models
{
    public class Product
    {
        public Guid ProductId { get; set; }
        public Guid CompanyId { get; set; }
        public string Title { get; set; }
        public string Explanation { get; set; }

        // Navigation prop's: defines the relationship btw the tables 
        public Company Company { get; set; } // many to one
        public ICollection<ProductClient> ProductClients { get; set; } // one to many
    }
}
