using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace CompanyHubService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserService userService;
        private readonly UserManager<User> userManager;
        private readonly CompanyHubDbContext dbContext;
        private readonly NotificationService notificationService;

        private readonly BlobServiceClient blobServiceClient;

        private readonly IConfiguration _configuration;
        private readonly EmailService emailService;

        private readonly AuthService authService;

        public UserController(UserService userService, UserManager<User> userManager, CompanyHubDbContext dbContext, NotificationService notificationService, BlobServiceClient blobServiceClient, IConfiguration configuration, EmailService emailService, AuthService authService)
        {
            this.userService = userService;
            this.userManager = userManager;
            this.dbContext = dbContext;
            this.notificationService = notificationService;
            this.blobServiceClient = blobServiceClient;
            _configuration = configuration;
            this.emailService = emailService;
            this.authService = authService;
        }


        [HttpGet("GetAllUsers")]
        [Authorize]
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
                UserName = user.UserName,
                Bio = user.Bio,
                Email = user.Email,
                Position = user.Position,
                LinkedInUrl = user.LinkedInUrl
            };

            return Ok(profile);
        }

        [HttpPut("UpdateUserProfile")]
        [Authorize]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UserProfileDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 1) Find the current user id from the token
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized(new { Message = "Invalid token." });

            // 2) Load the user entity
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { Message = "User not found." });

            // 3) Apply updates
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.Bio = model.Bio;
            user.Position = null;
            user.LinkedInUrl = model.LinkedInUrl;

            // 4) Save
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(new { Errors = result.Errors });

            return Ok(new { Message = "Profile updated successfully." });
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
            user.Bio = dto.Bio ?? user.Bio;
            user.Position = dto.Position ?? user.Position;
            user.LinkedInUrl = dto.LinkedInUrl ?? user.LinkedInUrl;

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

        [HttpPost("AcceptInvitation")]
        [Authorize]
        public async Task<IActionResult> AcceptInvitation([FromBody] Guid invitationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await userManager.FindByIdAsync(userId);

            var invitation = await dbContext.CompanyInvitations
                .FirstOrDefaultAsync(i => i.InvitationId == invitationId && i.UserId == userId && !i.Accepted && !i.Rejected);

            if (invitation == null)
                return BadRequest(new { Message = "Invitation not found or already processed." });

            // Check the users role
            var isUser = await userManager.IsInRoleAsync(user, "User");

            if (isUser)
            {
                // Re-send confirmation email
                var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = WebUtility.UrlEncode(token);
                var confirmationLink = $"{_configuration["AppSettings:ClientUrl"]}/confirm-email?userId={user.Id}&token={encodedToken}";

                await emailService.SendEmailAsync(user.Email, "Confirm Your Email",
                $@"
                <html>
                <body>
                    <h2>Email Confirmation Required</h2>
                    <p>Please confirm your email by clicking the link below:</p>
                    <a href='{confirmationLink}'>Confirm Email</a>
                    <p>{confirmationLink}</p>
                </body>
                </html>");

                return BadRequest(new { Message = "Please confirm your email before accepting the invitation. A confirmation email has been sent." });
            }

            // Add user to company
            dbContext.UserCompanies.Add(new UserCompany
            {
                UserId = userId,
                CompanyId = invitation.CompanyId
            });

            // Mark invitation as accepted
            invitation.Accepted = true;

            await dbContext.SaveChangesAsync();

            var isVerifiedUser = await userManager.IsInRoleAsync(user, "VerifiedUser");

            if (isVerifiedUser)
            {
                await userManager.RemoveFromRoleAsync(user, "VerifiedUser");
                await userManager.AddToRoleAsync(user, "Root");
            }

            return Ok(new { Message = "Invitation accepted. You have been added to the company." });
        }

        [HttpPost("RejectInvitation")]
        [Authorize]
        public async Task<IActionResult> RejectInvitation([FromBody] Guid invitationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var invitation = await dbContext.CompanyInvitations
                .FirstOrDefaultAsync(i => i.InvitationId == invitationId && i.UserId == userId && !i.Accepted && !i.Rejected);

            if (invitation == null)
                return BadRequest(new { Message = "Invitation not found or already processed." });

            // Mark as rejected
            invitation.Rejected = true;

            await dbContext.SaveChangesAsync();

            return Ok(new { Message = "Invitation rejected." });
        }

        [HttpGet("MyInvitations")]
        [Authorize]
        public async Task<IActionResult> GetMyInvitations()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var invitations = await dbContext.CompanyInvitations
                .Where(i => i.UserId == userId && !i.Accepted && !i.Rejected)
                .Include(i => i.Company)
                .Select(i => new
                {
                    i.InvitationId,
                    i.SentAt,
                    i.CompanyId,
                    CompanyName = i.Company.CompanyName
                })
                .ToListAsync();

            return Ok(invitations);
        }

        [HttpPost("SendSupportMessage")]
        public IActionResult SendSupportMessage([FromBody] SupportRequestDTO request)
        {
            try
            {

                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("compediacorp@gmail.com", _configuration["Smtp:Password"]),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(request.Email),
                    Subject = $"Compedia Support Message from user: {request.Name}",
                    Body = request.Message,
                    IsBodyHtml = false,
                };
                mailMessage.To.Add("compediacorp@gmail.com");

                smtpClient.Send(mailMessage);

                return Ok(new { message = "Message sent successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to send message.", error = ex.Message });
            }
        }

        [HttpGet("CheckTokenExpiration")]
        [Authorize]
        public IActionResult CheckTokenExpiration(string token)
        {
            // first validate the token
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;
            if (jwtToken == null)
                return Unauthorized(new { Message = "Invalid token." });

            // Check if the token is expired

            var expirationClaim = User.FindFirst("exp");
            if (expirationClaim == null)
                return Unauthorized(new { Message = "Invalid token." });

            var expirationDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expirationClaim.Value));
            var isExpired = expirationDate < DateTimeOffset.UtcNow;

            if (isExpired)
            {
                // Token is expired, refresh it
                return Unauthorized(new { Message = "Token expired." });
            }

            return Ok(new { Message = "Token is valid." });
        }

        [HttpPost("RefreshToken")]
        [Authorize]
        public async Task<IActionResult> RefreshToken()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Invalid token." });

            // Retrieve the user
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return Unauthorized(new { Message = "User not found." });

            // Generate a new token
            var newToken = await authService.GenerateJwtToken(user);

            return Ok(new { Token = newToken });
        }



    }
}

