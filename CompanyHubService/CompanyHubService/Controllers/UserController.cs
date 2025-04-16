using System.Security.Claims;
using Azure.Storage.Blobs;
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

        private readonly BlobServiceClient blobServiceClient;

        public UserController(UserService userService, UserManager<User> userManager, CompanyHubDbContext dbContext, NotificationService notificationService, BlobServiceClient blobServiceClient)
        {
            this.userService = userService;
            this.userManager = userManager;
            this.dbContext = dbContext;
            this.notificationService = notificationService;
            this.blobServiceClient = blobServiceClient;

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

        [HttpGet("GetUserProfileByUsername/{username}")]
        [Authorize]
        public async Task<IActionResult> GetUserProfileByUsername(string username)
        {
            var user = await userManager.FindByNameAsync(username);
            if (user == null)
                return NotFound(new { Message = "User not found." });

            var profile = new UserProfileDTO
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhotoUrl = user.PhotoUrl,
                PhoneNumber = user.PhoneNumber,
                UserName = user.UserName
            };

            return Ok(profile);
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

        [HttpGet("GetNotifications")]
        [Authorize]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await notificationService.GetUnreadNotificationsAsync(userId);

            return Ok(notifications);
        }

        [HttpPut("UpdateProfile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound(new { Message = "User not found." });

            user.FirstName = dto.FirstName ?? user.FirstName;
            user.LastName = dto.LastName ?? user.LastName;
            user.PhoneNumber = dto.PhoneNumber ?? user.PhoneNumber;

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(new { Errors = result.Errors });

            return Ok(new { Message = "Profile updated successfully." });
        }

        [HttpPost("UploadProfilePhoto")]
        [Authorize]
        public async Task<IActionResult> UploadProfilePhoto(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return Unauthorized();

            // Generate a unique blob name: profile-photos/{userId}/{guid}.ext
            var extension = Path.GetExtension(file.FileName);
            var blobName = $"profile-photos/{userId}/{Guid.NewGuid()}{extension}";

            try
            {
                // Connect to your blob container
                var containerClient = blobServiceClient.GetBlobContainerClient("profile-photos");
                await containerClient.CreateIfNotExistsAsync();
                var blobClient = containerClient.GetBlobClient(blobName);

                using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, overwrite: true);

                // Save the blob URL to user
                user.PhotoUrl = blobClient.Uri.ToString();
                var result = await userManager.UpdateAsync(user);

                if (!result.Succeeded)
                    return BadRequest(new { message = "User update failed.", errors = result.Errors });

                return Ok(new { message = "Photo uploaded successfully.", photoUrl = user.PhotoUrl });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Blob upload failed: {ex.Message}");
                return StatusCode(500, new { message = "Internal error uploading photo." });
            }
        }



    }
}
