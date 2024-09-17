using CompanyHubService.Data;
using CompanyHubService.Models;
using Microsoft.EntityFrameworkCore;

namespace CompanyHubService.Services
{
    public class UserService
    {
        private CompanyHubDbContext _dbContext { get; set; }
        public UserService(CompanyHubDbContext dbContext) {
            _dbContext = dbContext;
        }
        
        public async Task<List<User>> GetAllUsers()
        {
            var users = await _dbContext.Users.ToListAsync();

            return users;
        }
    }
}
