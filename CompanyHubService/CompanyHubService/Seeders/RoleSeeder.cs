using Microsoft.AspNetCore.Identity;

namespace CompanyHubService.Seeders
{
    public class RoleSeeder
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public RoleSeeder(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task InitializeRolesAsync()
        {
            var roles = new[] { "Root", "Admin" };

            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
    }
}
