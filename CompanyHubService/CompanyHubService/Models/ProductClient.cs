namespace CompanyHubService.Models
{
    public class ProductClient
    {
        public int ProductClientId { get; set; } // Primary Key
        public Guid ProductId { get; set; } // Foreign Key to Product
        public Guid ClientCompanyId { get; set; } // Foreign Key to Company

        // nav prop's
        public Product Product { get; set; } // many to one
        public Company ClientCompany { get; set; } // many to one
    }
}