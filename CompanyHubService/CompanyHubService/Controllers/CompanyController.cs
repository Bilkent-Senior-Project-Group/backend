using System.Security.Claims;
using CompanyHubService.Data;
using CompanyHubService.DTOs;
using CompanyHubService.Models;
using CompanyHubService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class CompanyController : ControllerBase
{
    private readonly CompanyService companyService;
    private readonly UserService userService;

    private readonly UserManager<User> userManager;

    private readonly CompanyHubDbContext dbContext;

    public CompanyController(CompanyService companyService, UserService userService, UserManager<User> userManager, CompanyHubDbContext dbContext)
    {
        this.companyService = companyService;
        this.userService = userService;
        this.userManager = userManager;
        this.dbContext = dbContext;
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

        var user = await userManager.FindByIdAsync(userId);

        if (!user.EmailConfirmed) // ✅ Check if email is confirmed
        {
            return BadRequest(new { Message = "Your email must be confirmed before creating a company." });
        }

        // Define the role as CompanyAdmin for the creator
        var roleId = "e9fe2584-94a2-4c37-90d8-437041c07ab8"; //ADMIN (we might )
                                                             // 


        var result = await companyService.CreateCompanyAsync(request, userId, roleId);

        if (!result)
        {
            return BadRequest(new { Message = "Failed to create company." });
        }

        return Ok(new { Message = "Company successfully created." });
    }

    [HttpGet("GetCompany/{companyId}")]
    public async Task<IActionResult> GetCompany(Guid companyId)
    {
        var company = await dbContext.Companies
            .Where(c => c.CompanyId == companyId)
            .Include(c => c.Projects) // ✅ Include projects under the company
            .FirstOrDefaultAsync();

        if (company == null)
        {
            return NotFound(new { Message = "Company not found." });
        }

        var companyDTO = new CompanyProfileDTO
        {
            CompanyId = company.CompanyId,
            Name = company.CompanyName,
            Description = company.Description,
            FoundedYear = company.FoundedYear,
            Address = company.Address,
            Specialties = company.Specialties,
            Industries = company.Industries?.Split(", ").ToList() ?? new List<string>(),
            Location = company.Location,
            Website = company.Website,
            Verified = company.Verified ? 1 : 0,
            CompanySize = company.CompanySize,
            ContactInfo = company.ContactInfo,
            CoreExpertise = company.CoreExpertise?.Split(", ").ToList() ?? new List<string>(),
            Projects = company.Projects.Select(p => new ProjectDTO
            {
                ProjectId = p.ProjectId,
                ProjectName = p.ProjectName,
                Description = p.Description,
                TechnologiesUsed = p.TechnologiesUsed.Split(", ").ToList(),
                Industry = p.Industry,
                ClientType = p.ClientType,
                Impact = p.Impact,
                Date = p.Date,
                ProjectUrl = p.ProjectUrl
            }).ToList()
        };

        return Ok(companyDTO);
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

    [HttpPost("ApproveCompany/{companyId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveCompany(Guid companyId)
    {
        var company = await dbContext.Companies.FindAsync(companyId);

        if (company == null)
        {
            return NotFound(new { Message = "Company not found." });
        }

        if (company.Verified)
        {
            return BadRequest(new { Message = "Company is already approved." });
        }

        company.Verified = true;
        await dbContext.SaveChangesAsync();

        return Ok(new { Message = $"Company '{company.CompanyName}' has been approved." });
    }

}
