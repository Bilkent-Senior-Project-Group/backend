using Microsoft.EntityFrameworkCore;
using CompanyHubService.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace CompanyHubService.Data
{
    public class CompanyHubDbContext : DbContext
    {
        public CompanyHubDbContext(DbContextOptions<CompanyHubDbContext> options)
            : base(options)
        {
        }

        // DbSet for User model
        public DbSet<User> Users { get; set; }

        // DbSet for Company model
        public DbSet<Company> Companies { get; set; }

        // DbSet for UserCompany model
        public DbSet<UserCompany> UserCompanies { get; set; }

        // Configuring relationships and table properties
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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

            // You can add more configurations here if necessary.
        }
    }
}
