using CompanyHubService.DTOs;
using CompanyHubService.Models;
using CompanyHubService.Services;
using CompanyHubService.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly UserService _userService;
    private readonly UserManager<User> _userManager;

    public AccountController(AuthService authService, UserService userService, UserManager<User> userManager)
    {
        _authService = authService;
        _userService = userService;
        _userManager = userManager;
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

        var userDTO = new UserDTO
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            // Role???
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
}
