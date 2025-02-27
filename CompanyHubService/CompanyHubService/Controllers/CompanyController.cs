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

    // This is the one where the root user creates/adds a company by himself/herself
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


        var result = await companyService.CreateCompanyAsync(request, userId, roleId);

        if (!result)
        {
            return BadRequest(new { Message = "Failed to create company." });
        }

        return Ok(new { Message = "Company successfully created." });
    }

    [HttpPost("ModifyCompanyProfile")]
    //[Authorize]
    public async Task<IActionResult> ModifyCompanyProfile(CompanyProfileDTO companyProfileDTO)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { Message = "Invalid input parameters." });
        }

        var result = await companyService.ModifyCompanyProfileAsync(companyProfileDTO);

        if (!result)
        {
            return BadRequest(new { Message = "Failed to modify company profile." });
        }
        else
        {
            return Ok(new { Message = "Company profile is successfully modified." });
        }
    }

    [HttpGet("GetUsersOfCompany/{companyId}")]
    [Authorize] // Optional: Ensure only authorized users can access this
    public async Task<IActionResult> GetUsersOfCompany(Guid companyId)
    {
        var users = await companyService.GetUsersOfCompanyAsync(companyId);

        if (users == null || !users.Any())
        {
            return NotFound(new { Message = "No users found for the company." });
        }

        return Ok(users);
    }

    [HttpGet("GetCompaniesOfUser/{userId}")]
    [Authorize] // Optional: Ensure only authorized users can access this
    public async Task<IActionResult> GetCompaniesOfUser(string userId)
    {
        var companies = await companyService.GetCompaniesOfUserAsync(userId);

        if (companies == null || !companies.Any())
        {
            return NotFound(new { Message = "No company found for the user." });
        }

        return Ok(companies);
    }

    //This is the one where company is added from a given json file with one company
    [HttpPost("AddCompany")]
    public async Task<IActionResult> AddCompany([FromBody] CompanyProfileDTO companyDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { Message = "Invalid input parameters." });
        }

        var result = await companyService.AddCompanyAsync(companyDto);

        if (!result)
        {
            return BadRequest(new { Message = "Failed to add company." });
        }

        return Ok(new { Message = "Company successfully added." });
    }

    //This is the one where company is added from a given json file with muitple companies
    [HttpPost("BulkAddCompanies")]
    public async Task<IActionResult> BulkAddCompanies([FromBody] Dictionary<string, CompanyProfileDTO> jsonCompanies)
    {
        if (jsonCompanies == null || jsonCompanies.Count == 0)
        {
            return BadRequest(new { message = "No companies found in the request." });
        }

        // Convert dictionary to a list of CompanyProfileDTO
        var bulkCompanies = new BulkCompanyInsertDTO
        {
            Companies = jsonCompanies.Select(entry =>
            {
                var companyDto = entry.Value;
                companyDto.Website = entry.Key;  // The JSON key is the website
                return companyDto;
            }).ToList()
        };

        var result = await companyService.BulkAddCompaniesAsync(bulkCompanies);

        if (result)
            return Ok(new { message = "All companies added successfully." });

        return BadRequest(new { message = "Failed to add companies." });
    }
}
