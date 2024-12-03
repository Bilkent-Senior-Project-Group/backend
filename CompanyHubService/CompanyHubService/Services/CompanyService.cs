using CompanyHubService.Data;
using CompanyHubService.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace CompanyHubService.Services
{
    public class CompanyService
    {
        private CompanyHubDbContext _dbContext { get; set; }
        private UserService userService { get; set; }

        public CompanyService(CompanyHubDbContext dbContext, UserService userService)
        {
            _dbContext = dbContext;
            this.userService = userService;
        }

        public async Task<bool> CreateCompanyAsync(string companyName, int foundationYear, string address, string userId, string roleId)
        {
            if (string.IsNullOrEmpty(companyName) || string.IsNullOrEmpty(address))
            {
                return false;
            }

            var company = new Company
            {
                CompanyId = Guid.NewGuid(),
                CompanyName = companyName,
                FoundationYear = foundationYear,
                CompanySize = 1,
                Address = address,
                SignupDate = DateTime.UtcNow
            };


            _dbContext.Companies.Add(company);

            var userCompany = new UserCompany
            {
                UserId = userId,
                CompanyId = company.CompanyId,
                RoleId = roleId
            };

            _dbContext.UserCompanies.Add(userCompany);
            await _dbContext.SaveChangesAsync();

            return true;
        }
    }
}