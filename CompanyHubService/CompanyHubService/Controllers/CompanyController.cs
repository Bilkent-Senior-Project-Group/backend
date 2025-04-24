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

        private readonly AnalyticsService analyticsService;

        private readonly BlobStorageService blobStorageService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly NotificationService notificationService;

        private readonly EmailService emailService;

        public CompanyController(IPdfExtractionService pdfExtractionService, CompanyService companyService, UserService userService, UserManager<User> userManager, CompanyHubDbContext dbContext, BlobStorageService blobStorageService, AnalyticsService analyticsService, IHttpContextAccessor httpContextAccessor, NotificationService notificationService, EmailService emailService)
        {
            _httpContextAccessor = httpContextAccessor;
            this.pdfExtractionService = pdfExtractionService;
            this.companyService = companyService;
            this.userService = userService;
            this.userManager = userManager;
            this.dbContext = dbContext;
            this.blobStorageService = blobStorageService;
            this.analyticsService = analyticsService;
            this.notificationService = notificationService;
            this.emailService = emailService;
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
        [Authorize]
        public async Task<IActionResult> GetCompany(string companyName)
        {
            Console.WriteLine($"Is Authenticated: {User.Identity.IsAuthenticated}");
            Console.WriteLine($"Authentication Type: {User.Identity.AuthenticationType}");

            foreach (var claim in User.Claims)
            {
                Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
            }
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
            .Include(pc => pc.Project)
                .ThenInclude(p => p.ServiceProjects)
                    .ThenInclude(sp => sp.Service)
            .Include(pc => pc.ClientCompany)
            .Include(pc => pc.ProviderCompany)
            .Select(pc => new ProjectViewDTO
            {
                ProjectId = pc.Project.ProjectId,
                ProjectName = pc.Project.ProjectName,
                Description = pc.Project.Description,
                TechnologiesUsed = pc.Project.TechnologiesUsed != null
                ? pc.Project.TechnologiesUsed.Split(",", StringSplitOptions.None).ToList()
                : new List<string>(),
                ClientType = pc.Project.ClientType,
                StartDate = pc.Project.StartDate,
                CompletionDate = pc.Project.CompletionDate ?? DateTime.MinValue,
                IsOnCompedia = pc.Project.IsOnCompedia,
                IsCompleted = pc.Project.IsCompleted,
                ProjectUrl = pc.Project.ProjectUrl,
                ClientCompanyName = pc.IsClient == 0 ? pc.OtherCompanyName : pc.IsClient == 1 ? pc.ClientCompany.CompanyName : pc.ClientCompany.CompanyName,
                ProviderCompanyName = pc.IsClient == 0 ? pc.ProviderCompany.CompanyName : pc.IsClient == 1 ? pc.OtherCompanyName : pc.ProviderCompany.CompanyName,
                Services = pc.Project.ServiceProjects.Select(sp => new ServiceDTO
                {
                    Id = sp.Service.Id,
                    Name = sp.Service.Name
                }).ToList()
            })
            .ToListAsync();


            var services = await dbContext.ServiceCompanies
            .Where(sc => sc.CompanyId == company.CompanyId)
            .Include(sc => sc.Service)
                .ThenInclude(s => s.Industry)
            .Select(sc => new ServiceIndustryViewDTO
            {
                Id = sc.Service.Id,
                ServiceName = sc.Service.Name,
                IndustryId = sc.Service.IndustryId,
                IndustryName = sc.Service.Industry.Name,
                Percentage = sc.Percentage
            })
            .ToListAsync();

            var cityCountry = await dbContext.CitiesAndCountries
                .Where(cc => cc.ID == company.Location)
                .FirstOrDefaultAsync();

            var providerProjectIds = await dbContext.ProjectCompanies
            .Where(pc => pc.ProviderCompanyId == company.CompanyId)
            .Select(pc => pc.ProjectId)
            .ToListAsync();

            var totalReviewsAsProvider = await dbContext.Reviews
            .CountAsync(r => providerProjectIds.Contains(r.ProjectId));


            var companyDTO = new CompanyProfileDTO
            {
                CompanyId = company.CompanyId,
                Name = company.CompanyName,
                Description = company.Description,
                FoundedYear = company.FoundedYear,
                Address = company.Address,
                Location = company.Location,
                City = cityCountry?.City,
                Country = cityCountry?.Country,
                Website = company.Website,
                Verified = company.Verified ? 1 : 0,
                CompanySize = company.CompanySize,
                Phone = company.Phone,
                Email = company.Email,
                OverallRating = company.OverallRating,
                Projects = projects,
                Services = services,
                LogoUrl = company.LogoUrl,
                TotalReviews = totalReviewsAsProvider
            };

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            Console.WriteLine($"User ID: {userId}");

            await analyticsService.InsertProfileViewAsync(new ProfileViewDTO
            {
                VisitorUserId = userId,
                CompanyId = company.CompanyId,
                ViewDate = DateTime.UtcNow,
                FromWhere = 0
            });


            return Ok(companyDTO);
        }

        [HttpPost("ModifyCompanyProfile")]
        [Authorize(Roles = "Root, Admin")] // Maybe VerifiedUser can modify their company profile too. Admin might be able to modify any company profile
        public async Task<IActionResult> ModifyCompanyProfile(CompanyProfileModifyDTO companyProfileDTO)
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
        public async Task<IActionResult> GetCompaniesOfUser(string userId)
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
                .OrderByDescending(c => c.FoundedYear)
                .Take(10)
                .Include(c => c.ServiceCompanies)
                    .ThenInclude(sc => sc.Service)
                        .ThenInclude(s => s.Industry)
                .ToListAsync();

            var locationMap = await dbContext.CitiesAndCountries.ToDictionaryAsync(cc => cc.ID, cc => cc);

            var result = companies.Select(c => new CompanyProfileDTO
            {
                CompanyId = c.CompanyId,
                Name = c.CompanyName,
                Description = c.Description,
                Location = c.Location,
                City = locationMap.ContainsKey(c.Location) ? locationMap[c.Location].City : null,
                Country = locationMap.ContainsKey(c.Location) ? locationMap[c.Location].Country : null,
                CompanySize = c.CompanySize,
                Services = c.ServiceCompanies.Select(sc => new ServiceIndustryViewDTO
                {
                    Id = sc.Service.Id,
                    ServiceName = sc.Service.Name,
                    IndustryId = sc.Service.IndustryId,
                    IndustryName = sc.Service.Industry.Name,
                    Percentage = sc.Percentage
                }).ToList()
            }).ToList();

            return Ok(result);
        }


        [HttpPost("FreeTextSearch")]
        public async Task<IActionResult> FreeTextSearch(FreeTextSearchDTO textQuery)
        {
            if (string.IsNullOrEmpty(textQuery.searchQuery))
            {
                return BadRequest(new { Message = "Empty search" });
            }
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            Console.WriteLine($"Current User ID: {currentUserId}");



            // Get the raw JSON string from the service
            string rawJsonResult = await companyService.FreeTextSearchAsync(textQuery, currentUserId);



            // Return it as ContentResult with proper content type
            return Content(rawJsonResult, "application/json");
        }

        [HttpPost("UploadLogo/{companyId}")]
        [Consumes("multipart/form-data")] // for swagger test
        public async Task<IActionResult> UploadCompanyLogo(Guid companyId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var company = await dbContext.Companies.FindAsync(companyId);
            if (company == null)
                return NotFound("Company not found.");

            var fileName = $"{companyId}_{file.FileName}";
            using var stream = file.OpenReadStream();
            var logoUrl = await blobStorageService.UploadLogoAsync(stream, fileName);

            company.LogoUrl = logoUrl;
            await dbContext.SaveChangesAsync();

            return Ok(new { Message = "Logo uploaded successfully.", LogoUrl = logoUrl });
        }

        [HttpDelete("DeleteLogo/{companyId}")]
        [Authorize(Roles = "Root")]
        public async Task<IActionResult> DeleteCompanyLogo(Guid companyId)
        {
            var company = await dbContext.Companies.FindAsync(companyId);
            if (company == null)
                return NotFound("Company not found.");

            var defaultLogoUrl = "https://azurelogo.blob.core.windows.net/company-logos/defaultcompany.png";

            // If logo is already the default one, don't proceed
            if (company.LogoUrl == defaultLogoUrl)
                return BadRequest(new { Message = "Company already has the default logo." });

            company.LogoUrl = defaultLogoUrl;
            await dbContext.SaveChangesAsync();

            return Ok(new { Message = "Logo reset to default." });
        }


        [HttpGet("SearchCompaniesByName")]
        public async Task<IActionResult> SearchCompaniesByName([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Query is required.");

            var results = await dbContext.Companies
                .Where(c => c.CompanyName.Contains(query))
                .Select(c => new
                {
                    c.CompanyId,
                    c.CompanyName
                })
                .Take(10)
                .ToListAsync();

            return Ok(results);
        }

        [HttpGet("GetAllServices")]
        public async Task<IActionResult> GetAllServices()
        {
            var services = await dbContext.Services.Include(q => q.Industry).GroupBy(q => q.IndustryId).ToListAsync();

            if (services.Any())
            {
                return Ok(services);
            }
            else
            {
                return BadRequest("Services can't be returned");
            }
        }

        [HttpGet("LocationSearch")]
        public async Task<ActionResult<IEnumerable<CitiesAndCountries>>> SearchLocations(string term)
        {
            if (string.IsNullOrEmpty(term) || term.Length < 2)
            {
                return new List<CitiesAndCountries>();
            }

            // Convert the search term to lowercase for case-insensitive search
            var searchTerm = term.ToLower();

            // Search both City and Country fields and return the complete rows
            var results = await dbContext.CitiesAndCountries
                .Where(c => c.City.ToLower().Contains(searchTerm) ||
                            c.Country.ToLower().Contains(searchTerm))
                .Take(20) // Limit results to prevent large result sets
                .ToListAsync();

            return results;
        }

        [HttpGet("GetProjectsOfCompany/{companyId}")]
        public async Task<IActionResult> GetProjectsOfCompany(Guid companyId)
        {
            var projectCompanies = await dbContext.ProjectCompanies
                .Where(pc => pc.ClientCompanyId == companyId || pc.ProviderCompanyId == companyId)
                .Include(pc => pc.Project)
                    .ThenInclude(p => p.ServiceProjects)
                        .ThenInclude(sp => sp.Service)
                .Include(pc => pc.ClientCompany)
                .Include(pc => pc.ProviderCompany)
                .ToListAsync();

            var result = projectCompanies.Select(pc => new ProjectViewDTO
            {
                ProjectId = pc.Project.ProjectId,
                ProjectName = pc.Project.ProjectName,
                Description = pc.Project.Description,
                TechnologiesUsed = pc.Project.TechnologiesUsed != null
                ? pc.Project.TechnologiesUsed.Split(",", StringSplitOptions.None).ToList()
                : new List<string>(),
                ClientType = pc.Project.ClientType,
                StartDate = pc.Project.StartDate,
                CompletionDate = pc.Project.CompletionDate ?? DateTime.MinValue,
                IsOnCompedia = pc.Project.IsOnCompedia,
                IsCompleted = pc.Project.IsCompleted,
                ProjectUrl = pc.Project.ProjectUrl,

                ClientCompanyName = pc.IsClient == 0
                    ? pc.OtherCompanyName
                    : pc.ClientCompany?.CompanyName,

                ProviderCompanyName = pc.IsClient == 1
                    ? pc.OtherCompanyName
                    : pc.ProviderCompany?.CompanyName,

                Services = pc.Project.ServiceProjects.Select(sp => new ServiceDTO
                {
                    Id = sp.Service.Id,
                    Name = sp.Service.Name
                }).ToList()
            }).ToList();

            return Ok(result);
        }

        [HttpPost("InviteUser")]
        [Authorize(Roles = "Root, Admin")]
        public async Task<IActionResult> InviteUser([FromBody] InviteUserDTO request)
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Check if the inviter is in the company
            bool isInviterInCompany = await dbContext.UserCompanies
                .AnyAsync(uc => uc.UserId == userId && uc.CompanyId == request.CompanyId);

            if (!isInviterInCompany)
            {
                return Forbid("You are not authorized to invite users to this company.");
            }

            var existingUser = await userManager.FindByEmailAsync(request.Email);

            if (existingUser != null)
            {
                // Check if user is already part of the company
                bool alreadyLinked = await dbContext.UserCompanies
                    .AnyAsync(uc => uc.UserId == existingUser.Id && uc.CompanyId == request.CompanyId);

                if (alreadyLinked)
                {
                    return BadRequest(new { Message = "User is already part of the company." });
                }

                // Check if an active invitation already exists
                bool alreadyInvited = await dbContext.CompanyInvitations
                    .AnyAsync(inv => inv.UserId == existingUser.Id && inv.CompanyId == request.CompanyId && !inv.Accepted && !inv.Rejected);

                if (alreadyInvited)
                {
                    return BadRequest(new { Message = "An invitation is already pending for this user." });
                }

                // Save invitation
                var invitation = new CompanyInvitation
                {
                    CompanyId = request.CompanyId,
                    UserId = existingUser.Id
                };

                dbContext.CompanyInvitations.Add(invitation);
                await dbContext.SaveChangesAsync();

                // Notify the user
                await notificationService.CreateNotificationAsync(
                    recipientId: existingUser.Id,
                    message: $"You've been invited to join a company.",
                    notificationType: "CompanyInvite",
                    url: $"/invitations?companyId={request.CompanyId}"
                );

                // Send email notification
                await emailService.SendEmailAsync(
                    existingUser.Email,
                    "You've been invited to join a company on Compedia",
                    $"Click the link below to accept the invitation:\n\nhttp://localhost:3000/signup?email={Uri.EscapeDataString(request.Email)}&companyId={request.CompanyId}"
                );

                return Ok(new { Message = "User exists. Notification and invitation sent." });
            }
            else
            {
                // User doesn't exist yet → just send email with invite link
                var inviteLink = $"http://localhost:3000/signup?email={Uri.EscapeDataString(request.Email)}&companyId={request.CompanyId}";

                await emailService.SendEmailAsync(
                    request.Email,
                    "You're invited to join a company on Compedia",
                    $"Click the link below to register and join the company:\n\n{inviteLink}"
                );

                return Ok(new { Message = "Invitation email sent to unregistered user." });
            }
        }


    }



}
