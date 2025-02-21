using Microsoft.EntityFrameworkCore;
using CompanyHubService.Models;
using System.Collections.Generic;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace CompanyHubService.Data
{
    public class CompanyHubDbContext : IdentityDbContext<User>
    {
        public CompanyHubDbContext(DbContextOptions<CompanyHubDbContext> options)
            : base(options)
        {
        }

        // DbSet for User model
        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }

        // DbSet for Company model
        public DbSet<Company> Companies { get; set; }

        // DbSet for UserCompany model
        public DbSet<UserCompany> UserCompanies { get; set; }

        public DbSet<UserClaim> UserClaims { get; set; }

        public DbSet<Product> Products { get; set; }
        public DbSet<ProductClient> ProductClients { get; set; }

        public DbSet<Notification> Notifications { get; set; }



        // Configuring relationships and table properties
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Company Config
            modelBuilder.Entity<Company>()
                .HasKey(c => c.CompanyId);

            // Defining the relationship between User and UserCompany
            modelBuilder.Entity<UserCompany>()
                .HasOne(uc => uc.User)
                .WithMany()
                .HasForeignKey(uc => uc.UserId);

            // Defining the relationship between Company and UserCompany
            modelBuilder.Entity<UserCompany>()
                .HasOne(uc => uc.Company)
                .WithMany()
                .HasForeignKey(uc => uc.CompanyId);

            modelBuilder.Entity<UserClaim>()
            .HasOne(uc => uc.Company)
            .WithMany()
            .HasForeignKey(uc => uc.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

            // Product configuration
            modelBuilder.Entity<Product>()
                .HasKey(p => p.ProductId); // Primary Key
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Company)
                .WithMany(c => c.Products) // One-to-Many relationship
                .HasForeignKey(p => p.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // ProductClient configuration
            modelBuilder.Entity<ProductClient>()
                .HasKey(pc => pc.ProductClientId); // Primary Key

            modelBuilder.Entity<ProductClient>()
                .HasOne(pc => pc.Product)
                .WithMany(p => p.ProductClients) // One-to-Many relationship
                .HasForeignKey(pc => pc.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductClient>()
                .HasOne(pc => pc.ClientCompany)
                .WithMany(c => c.ClientProductClients) // Assuming a Company.ClientProductClients property
                .HasForeignKey(pc => pc.ClientCompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Project Config
            modelBuilder.Entity<Project>()
                .HasKey(p => p.ProjectId);

            modelBuilder.Entity<Project>()
                .HasOne(p => p.Company)
                .WithMany(c => c.Projects)
                .HasForeignKey(p => p.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
