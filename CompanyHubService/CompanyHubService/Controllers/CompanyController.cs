using System.Security.Claims;
using CompanyHubService.Data;
using CompanyHubService.DTOs;
using CompanyHubService.Models;
using CompanyHubService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using iText.Layout.Element;
namespace CompanyHubService.Controllers
{

    [Route("api/[controller]")]
    [ApiController]


    public class CompanyController : ControllerBase
    {
        private readonly IPdfExtractionService pdfExtractionService;
        private readonly CompanyService companyService;
        private readonly UserService userService;
        private readonly UserManager<User> userManager;

        private readonly CompanyHubDbContext dbContext;

        public CompanyController(IPdfExtractionService pdfExtractionService, CompanyService companyService, UserService userService, UserManager<User> userManager, CompanyHubDbContext dbContext)
        {
            this.pdfExtractionService = pdfExtractionService;
            this.companyService = companyService;
            this.userService = userService;
            this.userManager = userManager;
            this.dbContext = dbContext;
        }

        [HttpPost("extract-from-pdf")]
        //[Authorize]
        public async Task<IActionResult> ExtractFromPdf(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest("File must be a PDF");

            try
            {
                using (var stream = file.OpenReadStream())
                {
                    var extractedData = await pdfExtractionService.ExtractCompanyDataFromPdf(stream);
                    return Ok(extractedData);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        // This is the one where the root user creates/adds a company by himself/herself
        [HttpPost("CreateCompany")]
        // Both the root user and the verifiedUser, maybe admin too can create a company (Admin can create a company on behalf of a user)
        [Authorize(Roles = "Root, VerifiedUser, Admin")]
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

            if (!user.EmailConfirmed) // This might be unnecessary as the user is already verified
            {
                return BadRequest(new { Message = "Your email must be confirmed before creating a company." });
                // send mail here (we can send the mail here or in the FRONTEND)
            }

            var result = await companyService.CreateCompanyAsync(request, userId);

            if (!result)
            {
                return BadRequest(new { Message = "Failed to create company." });
            }

            // Retrieve the newly created company's details
            var company = await dbContext.Companies
                .Where(c => c.CompanyName == request.CompanyName)
                .FirstOrDefaultAsync();

            if (company == null)
            {
                return BadRequest(new { Message = "Company created but failed to retrieve details." });
            }

            var companyDTO = new
            {
                companyId = company.CompanyId,
                companyName = company.CompanyName
            };

            return Ok(new { Message = "Company successfully created.", Data = companyDTO });
        }

        [HttpGet("GetCompany/{companyName}")]
        public async Task<IActionResult> GetCompany(string companyName)
        {
            var company = await dbContext.Companies
                .Where(c => c.CompanyName.Replace(" ", "") == companyName)
                .Include(c => c.Projects)
                    .ThenInclude(p => p.ProjectCompany) // Include ProjectCompany
                    .ThenInclude(pc => pc.ClientCompany) // Ensure ClientCompany is included
                .Include(c => c.Projects)
                    .ThenInclude(p => p.ProjectCompany)
                    .ThenInclude(pc => pc.ProviderCompany) // Ensure ProviderCompany is included
                .FirstOrDefaultAsync();

            if (company == null)
            {
                return NotFound(new { Message = "Company not found." });
            }

            var projects = await dbContext.ProjectCompanies
                .Where(pc => pc.ClientCompanyId == company.CompanyId || pc.ProviderCompanyId == company.CompanyId)
                .Select(pc => new ProjectDTO
                {
                    ProjectId = pc.Project.ProjectId,
                    ProjectName = pc.Project.ProjectName,
                    Description = pc.Project.Description,
                    TechnologiesUsed = pc.Project.TechnologiesUsed.Split(new[] { ", " }, StringSplitOptions.None).ToList(),
                    Industry = pc.Project.Industry,
                    ClientType = pc.Project.ClientType,
                    Impact = pc.Project.Impact,
                    StartDate = pc.Project.StartDate,
                    CompletionDate = pc.Project.CompletionDate,
                    IsOnCompedia = pc.Project.IsOnCompedia,
                    IsCompleted = pc.Project.IsCompleted,
                    ProjectUrl = pc.Project.ProjectUrl,
                    ClientCompanyName = pc.ClientCompany.CompanyName,
                    ProviderCompanyName = pc.ProviderCompany.CompanyName
                })
                .ToListAsync();

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
                Phone = company.Phone,
                Email = company.Email,
                OverallRating = company.OverallRating,
                CoreExpertise = company.CoreExpertise?.Split(", ").ToList() ?? new List<string>(),
                Projects = projects
            };

            return Ok(companyDTO);
        }

        [HttpPost("ModifyCompanyProfile")]
        [Authorize(Roles = "Root, Admin")] // Maybe VerifiedUser can modify their company profile too. Admin might be able to modify any company profile
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
        [Authorize(Roles = "Admin, Root, VerifiedUser")] // VerifiedUser and Root should be in the company to see the users
        public async Task<IActionResult> GetUsersOfCompany(Guid companyId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "User ID not found in token." });
            }

            var userCompany = await dbContext.UserCompanies
                .Where(uc => uc.UserId == userId && uc.CompanyId == companyId)
                .FirstOrDefaultAsync();

            if (userCompany == null && !User.IsInRole("Admin"))
            {
                return Unauthorized(new { Message = "You are not authorized to view the users of this company." });
            }

            var users = await companyService.GetUsersOfCompanyAsync(companyId);

            if (users == null || !users.Any())
            {
                return NotFound(new { Message = "No users found for the company." });
            }

            return Ok(users);
        }

        [HttpGet("GetCompaniesOfUser/{userId}")]
        [Authorize] // Only the user can see their companies (maybe Admin can see all companies) Maybe it can be changed later???
        public async Task<IActionResult> GetCompaniesOfUser([FromBody] string userId)
        {

            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { Message = "User ID is required." });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new { Message = "User ID not found in token." });
            }

            if (currentUserId != userId && !User.IsInRole("Admin"))
            {
                return Unauthorized(new { Message = "You are not authorized to view the companies of this user." });
            }

            var companies = await companyService.GetCompaniesOfUserAsync(userId);

            if (companies == null || !companies.Any())
            {
                return NotFound(new { Message = "No company found for the user." });
            }

            return Ok(companies);
        }

        [HttpGet("GetFeaturedCompanies")]
        public async Task<IActionResult> GetFeaturedCompanies()
        {
            var companies = await dbContext.Companies
                .Where(c => c.Verified)
                .OrderByDescending(c => c.FoundedYear) // we can change it to rating??
                .Take(10)
                .Select(c => new CompanyProfileDTO
                {
                    CompanyId = c.CompanyId,
                    Name = c.CompanyName,
                    Description = c.Description,
                    Location = c.Location,
                    CompanySize = c.CompanySize,
                    Specialties = c.Specialties
                })
                .ToListAsync();

            return Ok(companies);
        }

        [HttpGet("FreeTextSearch/{textQuery}")]
        public async Task<IActionResult> FreeTextSearch(string textQuery)
        {
            if (string.IsNullOrEmpty(textQuery))
            {
                return BadRequest(new { Message = "Empty search" });
            }

            // Get the raw JSON string from the service
            string rawJsonResult = await companyService.FreeTextSearchAsync(textQuery);


            // Return it as ContentResult with proper content type
            return Content(rawJsonResult, "application/json");
        }
    }
}
