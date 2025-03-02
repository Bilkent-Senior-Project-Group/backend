using CompanyHubService.Data;
using CompanyHubService.DTOs;
using CompanyHubService.Models;
using CompanyHubService.Services;
using CompanyHubService.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly UserService _userService;
    private readonly EmailService _emailService;
    private readonly UserManager<User> _userManager;

    private readonly CompanyHubDbContext _dbContext;

    public AccountController(AuthService authService, UserService userService, UserManager<User> userManager, EmailService emailService, CompanyHubDbContext dbContext)
    {
        _authService = authService;
        _userService = userService;
        _userManager = userManager;
        _emailService = emailService;
        _dbContext = dbContext;
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
            return Ok(new { Message = "Registration successful!" });
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


        var (result, token) = await _authService.LoginUserAsync(model.Email, model.Password); // var (result,token)

        if (!result.Succeeded)
        {
            return Unauthorized(new { Message = "Invalid email or password." });
        }

        else if (result.IsLockedOut)
        {
            return Unauthorized(new { Message = "Account is locked out. Please try again later." });
        }


        var companies = await _dbContext.UserCompanies
        .Where(uc => uc.UserId == user.Id)
        .Include(uc => uc.Company)
        .Select(uc => new CompanyDTO
        {
            CompanyId = uc.Company.CompanyId,
            CompanyName = uc.Company.CompanyName
        })
        .ToListAsync();


        var projects = await _dbContext.UserCompanies
        .Where(uc => uc.UserId == user.Id)
        .Include(uc => uc.Company)
        .ThenInclude(c => c.Projects)
        .SelectMany(uc => uc.Company.Projects.Select(p => new ProjectDTO // ✅ Explicit Mapping
        {
            ProjectId = p.ProjectId,
            ProjectName = p.ProjectName
        }))
        .ToListAsync();

        var userDTO = new UserDTO
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Companies = companies,
            Projects = projects
        };

        return Ok(new
        {
            // Need for message??
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
        var resetLink = $"{Request.Scheme}://{Request.Host}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email)}";

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

        var resetResult = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
        if (!resetResult.Succeeded)
        {
            return BadRequest(new { message = "Password reset failed.", errors = resetResult.Errors });
        }

        return Ok(new { message = "Password reset successful!" });
    }




}
