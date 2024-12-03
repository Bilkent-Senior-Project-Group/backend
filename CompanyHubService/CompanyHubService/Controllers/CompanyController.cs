using System.Security.Claims;
using CompanyHubService.DTOs;
using CompanyHubService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class CompanyController : ControllerBase
{
    private readonly CompanyService companyService;
    private readonly UserService userService;

    public CompanyController(CompanyService companyService, UserService userService)
    {
        this.companyService = companyService;
        this.userService = userService;
    }

    [HttpPost("CreateCompany")]
    [Authorize]
    public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyRequestDTO request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { Message = "Invalid input parameters." });
        }

        // Extract the user ID from the JWT token
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "User ID not found in token." });
        }

        // Define the role as CompanyAdmin for the creator
        var roleId = "e9fe2584-94a2-4c37-90d8-437041c07ab8"; //ADMIN
        // 

        var result = await companyService.CreateCompanyAsync(
            request.CompanyName,
            request.FoundationYear,
            request.Address,
            userId,
            roleId
        );

        if (!result)
        {
            return BadRequest(new { Message = "Failed to create company." });
        }

        return Ok(new { Message = "Company successfully created." });
    }

}
