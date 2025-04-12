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

        public DbSet<ProjectRequest> ProjectRequests { get; set; }
        public DbSet<ProjectCompany> ProjectCompanies { get; set; }

        public DbSet<Review> Reviews { get; set; }

        public DbSet<Service> Services { get; set; }
        public DbSet<Industry> Industries { get; set; }
        public DbSet<ServiceCompany> ServiceCompanies { get; set; }
        public DbSet<ServiceProject> ServiceProjects { get; set; }

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

            // One-to-One Relationship
            modelBuilder.Entity<ProjectCompany>()
                .HasOne(pc => pc.Project)
                .WithOne(p => p.ProjectCompany)
                .HasForeignKey<ProjectCompany>(pc => pc.ProjectId)
                .OnDelete(DeleteBehavior.Restrict); // Here we might also use Cascade but in order to prevent any error during migration i used Restrict

            modelBuilder.Entity<ProjectCompany>()
                .HasOne(pc => pc.ClientCompany)
                .WithMany()
                .HasForeignKey(pc => pc.ClientCompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProjectCompany>()
                .HasOne(pc => pc.ProviderCompany)
                .WithMany()
                .HasForeignKey(pc => pc.ProviderCompanyId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ProjectRequest>()
                .HasOne(pr => pr.ClientCompany)
                .WithMany()
                .HasForeignKey(pr => pr.ClientCompanyId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<ProjectRequest>()
                .HasOne(pr => pr.ProviderCompany)
                .WithMany()
                .HasForeignKey(pr => pr.ProviderCompanyId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure relationships if needed
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Project)
                .WithMany()
                .HasForeignKey(r => r.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId);

            // Industry configuration
            modelBuilder.Entity<Industry>()
                .HasKey(i => i.Id);

            // Service configuration
            modelBuilder.Entity<Service>()
                .HasKey(s => s.Id);

            modelBuilder.Entity<Service>()
                .HasOne(s => s.Industry)
                .WithMany()
                .HasForeignKey(s => s.IndustryId);

            // ServiceCompany configuration
            modelBuilder.Entity<ServiceCompany>()
                .HasKey(sc => sc.Id);

            modelBuilder.Entity<ServiceCompany>()
                .HasOne(sc => sc.Company)
                .WithMany()
                .HasForeignKey(sc => sc.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ServiceCompany>()
                .HasOne(sc => sc.Service)
                .WithMany()
                .HasForeignKey(sc => sc.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            // ServiceProject configuration
            modelBuilder.Entity<ServiceProject>()
                .HasKey(sp => sp.Id);

            modelBuilder.Entity<ServiceProject>()
                .HasOne(sp => sp.Project)
                .WithMany()
                .HasForeignKey(sp => sp.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ServiceProject>()
                .HasOne(sp => sp.Service)
                .WithMany()
                .HasForeignKey(sp => sp.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}