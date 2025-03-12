using System.Diagnostics.Eventing.Reader;
using CompanyHubService.Data;
using CompanyHubService.Models;
using CompanyHubService.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace CompanyHubService.Services
{
    public class AdminService
    {
        private CompanyHubDbContext _dbContext { get; set; }
        private UserManager<User> _userManager { get; set; }
        public AdminService(CompanyHubDbContext dbContext, UserManager<User> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
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

            var userCompany = await _dbContext.UserCompanies
            .Where(uc => uc.CompanyId == CompanyId)
            .OrderBy(uc => uc.AddedAt)
            .FirstOrDefaultAsync();

            if (userCompany != null)
            {
                var user = await _userManager.FindByIdAsync(userCompany.UserId);
                if (user != null)
                {
                    await _userManager.RemoveFromRoleAsync(user, "VerifiedUser");
                    await _userManager.AddToRoleAsync(user, "Root");
                }
            }

            return company;
        }


    }
}
