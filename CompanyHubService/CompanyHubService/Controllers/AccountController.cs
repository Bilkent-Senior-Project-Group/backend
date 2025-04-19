using CompanyHubService.Data;
using CompanyHubService.DTOs;
using CompanyHubService.Models;
using CompanyHubService.Services;
using CompanyHubService.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Abstractions;
using System.Threading.Tasks;
using System.Security.Claims;
using Newtonsoft.Json;
using System.Net;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly UserService _userService;
    private readonly EmailService _emailService;
    private readonly UserManager<User> _userManager;

    private readonly CompanyHubDbContext _dbContext;

    private readonly IUrlHelperFactory _urlHelperFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly IConfiguration _configuration;

    public AccountController(AuthService authService, UserService userService, UserManager<User> userManager, EmailService emailService, CompanyHubDbContext dbContext, IUrlHelperFactory iurlhelperfactory, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _authService = authService;
        _userService = userService;
        _userManager = userManager;
        _emailService = emailService;
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _urlHelperFactory = iurlhelperfactory;
        _configuration = configuration;
    }

    [HttpPost("Register")]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.RegisterUserAsync(model);

        if (result.Succeeded)
        {
            return Ok(new { Message = "Registration successful! Please verify your email." });
        }

        return BadRequest(result.Errors);
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { Message = "Invalid input." });
        }

        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user == null)
        {
            return Unauthorized(new { Message = "Invalid email or password." });
        }

        var (result, token) = await _authService.LoginUserAsync(model.Email, model.Password);

        if (!result.Succeeded)
        {
            return Unauthorized(new { Message = "Invalid email or password." });
        }
        else if (result.IsLockedOut)
        {
            return Unauthorized(new { Message = "Account is locked out. Please try again later." });
        }

        var isAdmin = false;
        var userDTO = new UserDTO();
        if (user.Id != "9f4d21df-e8ab-473a-889d-d2eeaee28b32")
        {

            var companies = await _dbContext.UserCompanies
                .Where(uc => uc.UserId == user.Id)
                .Include(uc => uc.Company)
                .ThenInclude(c => c.Projects)
                .Select(uc => new CompanyDTO
                {
                    CompanyId = uc.Company.CompanyId,
                    CompanyName = uc.Company.CompanyName,
                    Projects = _dbContext.ProjectCompanies
                        .Where(pc => pc.ClientCompanyId == uc.Company.CompanyId || pc.ProviderCompanyId == uc.Company.CompanyId)
                        .Select(pc => new ProjectDTO
                        {
                            ProjectId = pc.Project.ProjectId,
                            ProjectName = pc.Project.ProjectName,
                            Description = pc.Project.Description,
                            TechnologiesUsed = pc.Project.TechnologiesUsed.Split(new[] { ", " }, StringSplitOptions.None).ToList(),
                            ClientType = pc.Project.ClientType,
                            StartDate = pc.Project.StartDate,
                            CompletionDate = pc.Project.CompletionDate,
                            IsOnCompedia = pc.Project.IsOnCompedia,
                            IsCompleted = pc.Project.IsCompleted,
                            ProjectUrl = pc.Project.ProjectUrl,
                            ClientCompanyName = pc.ClientCompany.CompanyName,
                            ProviderCompanyName = pc.ProviderCompany.CompanyName,
                            //services eklenecek
                        }).ToList()
                })
                .ToListAsync();

            userDTO = new UserDTO
            {
                Id = user.Id,
                FirstName = user.FirstName,
                UserName = user.UserName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Companies = companies
            };
        }
        else
        {
            isAdmin = true;
            userDTO = new UserDTO
            {
                Id = user.Id,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Companies = null
            };
        }

        return Ok(new
        {
            isAdmin = isAdmin,
            Token = token,
            User = userDTO
        });
    }

    [Authorize]
    [HttpPost("Logout")]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutUserAsync();
        return Ok(new { Message = "Logout successful! Please clear your token on the client side." }); // i guess related with frontend
    }

    [HttpPost("CheckEmail")]
    public async Task<IActionResult> CheckEmail([FromBody] EmailCheckViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { Success = false, Message = "Invalid email format." });
        }

        bool isRegistered = await _userService.IsEmailRegisteredAsync(model.Email);

        return Ok(new { IsRegistered = isRegistered });
    }

    // For authenticated users
    [HttpPost("ChangePassword")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId);

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);

        if (!result.Succeeded)
            return BadRequest(new { Errors = result.Errors });

        return Ok(new { Message = "Password changed successfully." });
    }


    [HttpPost("ForgotPassword")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO model)
    {
        if (string.IsNullOrEmpty(model.Email))
        {
            return BadRequest(new { message = "Email is required." });
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return BadRequest(new { message = "User not found." });
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        // HERE WE SHOULD PASS THE LINK FOR THE RESET PASSWORD APGE
        var resetLink = $"https://localhost:3000/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email)}";


        //LATER DELETE THE TOKEN FROM EMAIL BODY 
        string emailBody = $@"
                <h3>Password Reset Request</h3>
                <p>Click the link below to reset your password:</p>
                <a href='{HtmlEncoder.Default.Encode(resetLink)}'>Reset Password</a>
                <p>{token}<p>
            ";

        bool emailSent = await _emailService.SendEmailAsync(user.Email, "Compedia Password Reset", emailBody);

        if (!emailSent)
        {
            return StatusCode(500, new { message = "Error sending email. Try again later." });
        }

        return Ok(new { message = "Password reset email sent successfully." });
    }

    [HttpPost("ResetPassword")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO model)
    {
        if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Token) || string.IsNullOrEmpty(model.NewPassword))
        {
            return BadRequest(new { message = "Invalid request." });
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return BadRequest(new { message = "User not found." });
        }

        var decodedToken = Uri.UnescapeDataString(model.Token);

        var resetResult = await _userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword);
        if (!resetResult.Succeeded)
        {
            return BadRequest(new { message = "Password reset failed.", errors = resetResult.Errors });
        }

        return Ok(new { message = "Password reset successful!" });
    }

    [HttpPost("ConfirmEmail")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
    {
        if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.Token))
        {
            return BadRequest("Invalid email confirmation parameters.");
        }

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        var decodedToken = Uri.UnescapeDataString(request.Token);

        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
        if (result.Succeeded)
        {
            if (await _userManager.IsInRoleAsync(user, "User"))
            {
                await _userManager.RemoveFromRoleAsync(user, "User");
                await _userManager.AddToRoleAsync(user, "VerifiedUser");
            }
            return Ok("Email confirmed successfully!");
        }
        else
        {
            return BadRequest("Email confirmation failed.");
        }
    }

    [HttpGet("debug-claims")]
    [Authorize] // Requires authentication
    public IActionResult DebugClaims()
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var claims = identity.Claims.Select(c => new { c.Type, c.Value }).ToList();

        // ✅ Log claims to the console (for debugging)
        Console.WriteLine(JsonConvert.SerializeObject(claims, Formatting.Indented));

        // ✅ Return claims as JSON in the response
        return Ok(claims);
    }

    // This endpoint will be used in the frontend to send a confirmation email when the user is not authorized to create a company
    // Inside a catch block, we can call this endpoint to send a confirmation email
    [HttpPost("SendConfirmationEmail")]
    [Authorize(Roles = "User")] // Only unverified users
    public async Task<IActionResult> SendConfirmationEmail()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return Unauthorized();

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebUtility.UrlEncode(token);
        var confirmationLink = $"{_configuration["AppSettings:ClientUrl"]}/confirm-email?userId={user.Id}&token={encodedToken}";

        await _emailService.SendEmailAsync(user.Email, "Confirm Your Email",
        $@"
        <html>
        <body>
            <h2>Email Confirmation Required</h2>
            <p>Please confirm your email by clicking the link below:</p>
            <a href='{confirmationLink}'>Confirm Email</a>
            <p>{confirmationLink}</p>
        </body>
        </html>");

        return Ok(new { Message = "A confirmation email has been sent. Please check your inbox." });
    }

}
