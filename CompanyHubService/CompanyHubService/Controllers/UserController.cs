using CompanyHubService.Models;
using CompanyHubService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CompanyHubService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserService userService;

        public UserController(UserService userService)
        {
            this.userService = userService;
        }


        [HttpGet("GetAllUsers")]
        public async Task<ActionResult<List<User>>> GetAllUsers()
        {
            var users = await userService.GetAllUsers();

            if (users == null)
            {
                return BadRequest();
            }
            return Ok(users);
        }

        [HttpGet("GetUserCompanies/{userId}")]
        public async Task<IActionResult> GetUserCompanies(string userId)
        {
            var companies = await userService.GetUserCompaniesAsync(userId);

            if (companies == null || !companies.Any())
            {
                return NotFound(new { Message = "No companies found for the user." });
            }

            return Ok(companies);
        }
    }
}
