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
        [Authorize(Roles = "Admin, Root")] // Maybe an invitation system can be added later. (InviteCompany table can be created)
        // The root user should select its company from a dropdown list of its companies. 
        public async Task<IActionResult> AddUserToCompany(string userId, string companyId) // Again DTO can be used.
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(companyId))
            {
                return BadRequest(new { Message = "Invalid input parameters." });
            }

            // Parse companyId to Guid
            if (!Guid.TryParse(companyId, out var companyGuid))
            {
                return BadRequest(new { Message = "Invalid Company ID." });
            }

            var result = await userService.AddUserToCompany(userId, companyGuid);

            if (result)
            {
                await notificationService.CreateNotificationAsync(
                recipientId: userId,
                message: "You have been added to a new company.",
                notificationType: "User",
                url: null
                );

                return Ok(new { Message = "User added and notified." });
            }

            return BadRequest(new { Message = "Failed to add user to company." });
        }

        [HttpGet("GetNotifications")]
        [Authorize]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await notificationService.GetUnreadNotificationsAsync(userId);

            return Ok(notifications);
        }
    }
}
