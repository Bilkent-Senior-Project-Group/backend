using CompanyHubService.Models;
using CompanyHubService.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]/[action]")]
public class AccountController : ControllerBase
{
    private readonly AuthService _authService;

    public AccountController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost]
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


    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {  
            return BadRequest(new { Message = "Invalid input." });
        }

        
        var (result, token) = await _authService.LoginUserAsync(model.Email, model.Password); // var (result,token)

        if (result.Succeeded)
        {
            return Ok(new
            {
                Message = "Login successful!",
                Token = token
            });
        }

        else if (result.IsLockedOut)
        {
            return Unauthorized(new { Message = "Account is locked out. Please try again later." });
        }

        return Unauthorized(new { Message = "Invalid login attempt." });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutUserAsync();
        return Ok(new { Message = "Logout successful! Please clear your token on the client side." }); // i guess related with frontend
    }
}
