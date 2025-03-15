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
                name = company.CompanyName
            };

            return Ok(new { Message = "Company successfully created.", Data = companyDTO });
        }

        [HttpGet("GetCompany/{companyName}")]
        public async Task<IActionResult> GetCompany(string companyName) // Maybe add [FromBody] later. Instead of getting it from an url
        {

            var company = await dbContext.Companies
                .Where(c => c.CompanyName.Replace(" ", "") == companyName)
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
                    StartDate = p.StartDate,
                    CompletionDate = p.CompletionDate,
                    IsOnCompedia = p.IsOnCompedia,
                    IsCompleted = p.IsCompleted,
                    ProjectUrl = p.ProjectUrl,
                    ClientCompany = new CompanyDTO
                    {
                        CompanyId = p.ProjectCompany.ClientCompany.CompanyId,
                        CompanyName = p.ProjectCompany.ClientCompany.CompanyName
                    },
                    ProviderCompany = p.ProjectCompany.ProviderCompany != null ? new CompanyDTO
                    {
                        CompanyId = p.ProjectCompany.ProviderCompany.CompanyId,
                        CompanyName = p.ProjectCompany.ProviderCompany.CompanyName
                    } : null
                }).ToList()
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
        public async Task<IActionResult> GetUsersOfCompany([FromBody] Guid companyId)
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

        [HttpPost("FreeTextSearch")] // Maybe this can be a GET request. Any user can search for companies
        [AllowAnonymous]
        public async Task<IActionResult> FreeTextSearch(string textQuery)
        {
            if (string.IsNullOrEmpty(textQuery))
            {
                return BadRequest(new { Message = "Empty search" });
            }

            var searchResult = await companyService.FreeTextSearchAsync(textQuery);

            return Ok(searchResult);
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



    }
}
