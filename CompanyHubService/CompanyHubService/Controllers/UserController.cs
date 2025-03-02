using System.Security.Claims;
using CompanyHubService.Data;
using CompanyHubService.DTOs;
using CompanyHubService.Models;
using CompanyHubService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace CompanyHubService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserService userService;
        private readonly UserManager<User> userManager;
        private readonly CompanyHubDbContext dbContext;
        private readonly NotificationService notificationService;

        public UserController(UserService userService, UserManager<User> userManager, CompanyHubDbContext dbContext, NotificationService notificationService)
        {
            this.userService = userService;
            this.userManager = userManager;
            this.dbContext = dbContext;
            this.notificationService = notificationService;
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

        [HttpGet("GetUserCompanies")]
        [Authorize]
        public async Task<IActionResult> GetUserCompanies()
        {
            // Extract user ID from JWT token
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "User ID not found in token." });
            }

            var companies = await userService.GetUserCompaniesAsync(userId);

            if (companies == null || !companies.Any())
            {
                return NotFound(new { Message = "No companies found for the user." });
            }

            return Ok(companies);
        }


        [HttpPost("AddUserToCompany")]
        public async Task<IActionResult> AddUserToCompany(string userId, string companyId, string roleId)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(companyId) || string.IsNullOrEmpty(roleId))
            {
                return BadRequest(new { Message = "Invalid input parameters." });
            }

            // Parse companyId to Guid
            if (!Guid.TryParse(companyId, out var companyGuid))
            {
                return BadRequest(new { Message = "Invalid Company ID." });
            }

            var result = await userService.AddUserToCompany(userId, companyGuid, roleId);

            if (result)
            {
                await notificationService.CreateNotificationAsync(
                recipientId: userId,
                message: "You have been added to a new company."
                );

                return Ok(new { Message = "User added and notified." });
            }

            return BadRequest(new { Message = "Failed to add user to company." });
        }

        [HttpGet("GetNotifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await notificationService.GetUnreadNotificationsAsync(userId);

            return Ok(notifications);
        }

        [HttpPost("ApproveUser/{userId}")]
        [Authorize(Roles = "Admin")] // Only admins can approve users
        public async Task<IActionResult> ApproveUser(string userId)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);

            return Ok(new { Message = "User email confirmed successfully." });
        }

    }
}
