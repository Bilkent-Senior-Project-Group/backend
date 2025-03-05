using System.Diagnostics.Eventing.Reader;
using CompanyHubService.Data;
using CompanyHubService.Models;
using CompanyHubService.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CompanyHubService.Services
{
    public class AdminService
    {
        private CompanyHubDbContext _dbContext { get; set; }
        public AdminService(CompanyHubDbContext dbContext)
        {
            _dbContext = dbContext;
        }

      

        public async Task<List<Company>> CompaniesToBeVerified()
        {
            var companies = await _dbContext.Companies.ToListAsync();
            companies = companies.Where(c => c.Verified == false).ToList();

            return companies;
        }

        public async Task<Company> VerifyCompany(Guid CompanyId)
        {
            var company = await _dbContext.Companies.FirstOrDefaultAsync(c => c.CompanyId == CompanyId);
            if (company == null)
            {
                return null;
            }

            company.Verified = true;
            await _dbContext.SaveChangesAsync();

            return company;
        }


    }
}
