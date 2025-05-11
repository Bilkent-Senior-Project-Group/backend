using CompanyHubService.Data;
using CompanyHubService.Models;
using CompanyHubService.Services;
using CompanyHubService.Views;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

public class AuthService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    private readonly RoleManager<IdentityRole> _roleManager;

    private readonly EmailService _emailService;
    private readonly IConfiguration _configuration;

    private readonly CompanyHubDbContext _dbContext;

    public AuthService(UserManager<User> userManager, SignInManager<User> signInManager, RoleManager<IdentityRole> roleManager, EmailService emailService, IConfiguration configuration, CompanyHubDbContext dbContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _emailService = emailService;
        _configuration = configuration;
        _dbContext = dbContext;
    }

    public async Task<IdentityResult> RegisterUserAsync(RegisterViewModel model)
    {
        var existingEmailUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingEmailUser != null)
        {
            var error = IdentityResult.Failed(new IdentityError
            {
                Code = "DuplicateEmail",
                Description = "Email is already registered."
            });
            return error;
        }

        var existingUsernameUser = await _userManager.FindByNameAsync(model.Username);
        if (existingUsernameUser != null)
        {
            var error = IdentityResult.Failed(new IdentityError
            {
                Code = "DuplicateUsername",
                Description = "Username is already taken."
            });
            return error;
        }

        User user = new User
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            UserName = model.Username,
            PhoneNumber = model.Phone,
            EmailConfirmed = false
        };

        var password = model.Password;
        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            return result;
        }

        if (result.Succeeded && model.CompanyId.HasValue)
        {
            _dbContext.UserCompanies.Add(new UserCompany
            {
                UserId = user.Id,
                CompanyId = model.CompanyId.Value
            });

            await _dbContext.SaveChangesAsync();
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var clientUrl = _configuration["AppSettings:ClientUrl"];
        var encodedToken = WebUtility.UrlEncode(token);
        var confirmationLink = $"{clientUrl}/confirm-email?userId={user.Id}&token={encodedToken}";



        await _emailService.SendEmailAsync(user.Email, "Confirm Your Email",
        $@"
        <html>
        <body>
            <h2>Email Confirmation</h2>
            <p>Please confirm your email by clicking the link below:</p>
            <a href='{confirmationLink}'>Confirm Email</a>
            <p>If the link doesn't work, copy and paste the following URL into your browser:</p>
            <p>{confirmationLink}</p>
        </body>
        </html>
        <p>{token}</p>
        ");

        if (!await _roleManager.RoleExistsAsync("User"))
        {
            await _roleManager.CreateAsync(new IdentityRole("User"));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, "User"); // Assign User role


        return roleResult;
    }


    /*public async Task<IdentityResult> RegisterUserAsyncAdmin(RegisterViewModel model)
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
        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            return result;
        }

        if (!await _roleManager.RoleExistsAsync("Admin"))
        {
            await _roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, "Admin");

        return roleResult;


    }*/


    public async Task<(Microsoft.AspNetCore.Identity.SignInResult, string)> LoginUserAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return (Microsoft.AspNetCore.Identity.SignInResult.Failed, null);
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName, password, isPersistent: false, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var token = await GenerateJwtToken(user);
            return (result, token);
        }

        return (result, null);
    }


    public async Task LogoutUserAsync()
    {
        await _signInManager.SignOutAsync();
    }

    public async Task<string> GenerateJwtToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.UserName)
        };

        // 🔹 Correctly await the asynchronous call
        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role)); // ✅ This ensures multiple roles work
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }


    // validate token by parametere token
    public async Task<ClaimsPrincipal> ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = _configuration["JwtSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = _configuration["JwtSettings:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            return tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
