using System.Diagnostics.Eventing.Reader;
using CompanyHubService.Data;
using CompanyHubService.Models;
using CompanyHubService.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CompanyHubService.Services
{
    public class UserService
    {
        private CompanyHubDbContext _dbContext { get; set; }
        public UserService(CompanyHubDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<User>> GetAllUsers()
        {
            var users = await _dbContext.Users.ToListAsync();

            return users;
        }

        // Method to check if email is already registered.
        public async Task<bool> IsEmailRegisteredAsync(string email)
        {
            return await _dbContext.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<List<CompanyDTO>> GetUserCompaniesAsync(string userId)
        {
            return await _dbContext.UserCompanies
                .Where(uc => uc.UserId == userId)
                .Include(uc => uc.Company)
                .Select(uc => new CompanyDTO
                {
                    CompanyId = uc.Company.CompanyId,
                    CompanyName = uc.Company.CompanyName
                })
                .ToListAsync();
        }

        public UserDTO MapToDTO(User user)
        {
            return new UserDTO
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
            };
        }


        // For now just for test purpose, but later we might only give authorizations for certain type of users for this.
        // Not working now for some reason.
        public async Task<bool> AddUserToCompany(string userId, Guid companyId, string roleId)
        {
            // Check if the user and company exist
            var userExists = await _dbContext.Users.AnyAsync(u => u.Id == userId);
            var company = await _dbContext.Companies.FirstOrDefaultAsync(c => c.CompanyId == companyId);

            if (!userExists || company == null)
            {
                return false;
            }

            // Check if the user is already in the company
            var alreadyExists = await _dbContext.UserCompanies.AnyAsync(uc => uc.UserId == userId && uc.CompanyId == companyId);
            if (alreadyExists)
            {
                return false;
            }

            // Add the user to the company
            var userCompany = new UserCompany
            {
                UserId = userId,
                CompanyId = companyId,
                RoleId = roleId
            };

            _dbContext.UserCompanies.Add(userCompany);

            // Increase the company size
            company.CompanySize += 1;
            _dbContext.Companies.Update(company);

            // Save changes
            await _dbContext.SaveChangesAsync();

            return true;
        }


    }
}
