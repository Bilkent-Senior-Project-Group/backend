using CompanyHubService.Models;
using CompanyHubService.Views;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

public class AuthService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public AuthService(UserManager<User> userManager, SignInManager<User> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<IdentityResult> RegisterUserAsync(RegisterViewModel model)
    {
        User user = new User
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            UserName = model.Username,
            PhoneNumber = model.Phone

        };
        var password = model.Password;
        return await _userManager.CreateAsync(user, password);
    }



    public async Task<(SignInResult, string)> LoginUserAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return (SignInResult.Failed, null);
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName, password, isPersistent: false, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var token = GenerateJwtToken(user);
            return (result, token);
        }

        return (result, null);
    }


    public async Task LogoutUserAsync()
    {
        await _signInManager.SignOutAsync();
    }

    public string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim(ClaimTypes.Name, user.UserName)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("JWT_SECRET_PLACEHOLDER")); 
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "Compedia",
            audience: "CompediaClient",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
