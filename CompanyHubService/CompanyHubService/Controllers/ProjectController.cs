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
public class ProjectController : ControllerBase
{
    private readonly CompanyService companyService;
    private readonly UserService userService;

    private readonly UserManager<User> userManager;

    private readonly CompanyHubDbContext dbContext;
    private readonly NotificationService notificationService;
    private readonly ProjectService projectService;

    public ProjectController(CompanyService companyService, UserService userService, UserManager<User> userManager, CompanyHubDbContext dbContext, NotificationService notificationService, ProjectService projectService)
    {
        this.companyService = companyService;
        this.userService = userService;
        this.userManager = userManager;
        this.dbContext = dbContext;
        this.notificationService = notificationService;
        this.projectService = projectService;
    }

    [HttpGet("GetProject/{projectId}")] // For, now any user can get a project. Later, we can add authorization.
    public async Task<IActionResult> GetProject(Guid projectId)
    {
        var projectDTO = await projectService.GetProjectAsync(projectId);
        if (projectDTO == null)
            return NotFound(new { Message = "Project not found." });

        return Ok(projectDTO);

    }

    [HttpPost("CreateProjectRequestByName")]
    [Authorize(Roles = "Root")]
    public async Task<IActionResult> CreateProjectRequestByName([FromBody] ProjectRequestByNameDTO request)
    {
        var clientCompany = await dbContext.Companies.FirstOrDefaultAsync(c => c.CompanyName == request.ClientCompanyName);
        var providerCompany = await dbContext.Companies.FirstOrDefaultAsync(c => c.CompanyName == request.ProviderCompanyName);

        if (clientCompany == null || providerCompany == null)
            return BadRequest(new { Message = "Client or provider company not found." });

        var converted = new ProjectRequestDTO
        {
            ClientCompanyId = clientCompany.CompanyId,
            ProviderCompanyId = providerCompany.CompanyId,
            ProjectName = request.ProjectName,
            Description = request.Description,
            TechnologiesUsed = request.TechnologiesUsed,
            ClientType = request.ClientType,
            Services = request.Services,
            Impact = request.Impact
        };

        var result = await projectService.CreateProjectRequestAsync(converted);
        if (result != "Project request sent successfully.")
            return BadRequest(new { Message = result });

        return Ok(new { Message = result });
    }


    [HttpGet("GetProjectRequest/{requestId}")]
    [Authorize(Roles = "Root, Admin, VerifiedUser")] // Only users that are in the either ClientCompany or ProviderCompany can get the project request.
    public async Task<IActionResult> GetProjectRequest(Guid requestId) // [FromBody] might be good
    {
        // Only users that are in the either ClientCompany or ProviderCompany can get the project request.
        var projectRequest = await dbContext.ProjectRequests
            .Include(pr => pr.ClientCompany)
            .Include(pr => pr.ProviderCompany)
            .FirstOrDefaultAsync(pr => pr.RequestId == requestId);

        if (projectRequest == null)
        {
            return NotFound(new { Message = "Project request not found." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "User ID not found in token." });
        }

        var userCompany = await dbContext.UserCompanies
            .Where(uc => uc.UserId == userId && uc.CompanyId == projectRequest.ClientCompanyId || uc.CompanyId == projectRequest.ProviderCompanyId)
            .FirstOrDefaultAsync();

        if (userCompany == null && !User.IsInRole("Admin"))
        {
            return Unauthorized(new { Message = "You are not authorized to view this project request." });
        }

        // Fetch full services (with Id + Name)
        var services = await dbContext.Services
            .Where(s => projectRequest.Services.Contains(s.Id))
            .ToListAsync();

        var projectRequestDTO = new ProjectRequestViewDTO
        {
            ClientCompanyId = projectRequest.ClientCompanyId,
            ProviderCompanyId = projectRequest.ProviderCompanyId ?? Guid.Empty,
            ProjectName = projectRequest.ProjectName,
            Description = projectRequest.Description,
            TechnologiesUsed = projectRequest.TechnologiesUsed?.Split(", ").ToList() ?? new List<string>(),
            ClientType = projectRequest.ClientType,
            Services = services.Select(s => new ServiceDTO
            {
                Id = s.Id,
                Name = s.Name
            }).ToList()
        };


        return Ok(projectRequestDTO);
    }

    [HttpGet("GetProjectRequestsOfCompany/{companyId}")]
    [Authorize(Roles = "Root, VerifiedUser, Admin")]
    public async Task<IActionResult> GetProjectRequestsOfCompany(Guid companyId)
    {

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var isUserInCompany = await dbContext.UserCompanies
        .AnyAsync(uc => uc.UserId == userId && uc.CompanyId == companyId);

        if (!isUserInCompany)
            return Forbid("You are not authorized to view this company's requests.");

        var requests = await projectService.GetProjectRequestsOfCompanyAsync(companyId);

        if (!requests.Any())
            return NotFound(new { Message = "No pending project requests for this company." });

        return Ok(requests);
    }


    [HttpPost("ApproveProjectRequest/{requestId}")]
    [Authorize(Roles = "Root")] // Only root user can approve a project request.
    public async Task<IActionResult> ApproveProjectRequest(Guid requestId, [FromBody] bool isAccepted) // Again have a look later if we should get the id from the link or not.
    {
        var result = await projectService.ApproveProjectRequestAsync(requestId, isAccepted);
        if (result.Contains("not found") || result.Contains("already been processed"))
            return BadRequest(new { Message = result });

        return Ok(new { Message = result });
    }

    [HttpPost("MarkProjectAsCompleted/{projectId}")]
    [Authorize(Roles = "Root, VerifiedUser")]
    public async Task<IActionResult> MarkProjectAsCompleted(Guid projectId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var project = await dbContext.Projects
            .Include(p => p.ProjectCompany)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);

        if (project == null)
            return NotFound("Project not found.");

        var userCompany = await dbContext.UserCompanies
            .FirstOrDefaultAsync(uc => uc.UserId == userId &&
                (uc.CompanyId == project.ProjectCompany.ClientCompanyId || uc.CompanyId == project.ProjectCompany.ProviderCompanyId));

        if (userCompany == null)
            return BadRequest("You are not part of this project.");

        var isClient = userCompany.CompanyId == project.ProjectCompany.ClientCompanyId;
        var isProvider = userCompany.CompanyId == project.ProjectCompany.ProviderCompanyId;

        if (isClient)
        {
            if (project.ClientMarkedCompleted)
                return BadRequest("Client has already marked this project as completed.");
            project.ClientMarkedCompleted = true;
        }
        else if (isProvider)
        {
            if (project.ProviderMarkedCompleted)
                return BadRequest("Provider has already marked this project as completed.");
            project.ProviderMarkedCompleted = true;
        }

        // ✅ If both sides marked completed, finalize the project
        if (project.ClientMarkedCompleted && project.ProviderMarkedCompleted)
        {
            project.IsCompleted = true;
            project.CompletionDate = DateTime.UtcNow;

            // ✅ Send notification to the Root user of the ClientCompany
            var rootUser = await dbContext.UserCompanies
                .Where(uc => uc.CompanyId == project.ProjectCompany.ClientCompanyId)
                .Include(uc => uc.User)
                .Where(uc => dbContext.UserRoles
                    .Any(ur => ur.UserId == uc.UserId && ur.RoleId == dbContext.Roles
                        .Where(r => r.Name == "Root")
                        .Select(r => r.Id)
                        .FirstOrDefault()))
                .Select(uc => uc.User)
                .FirstOrDefaultAsync();

            if (rootUser != null)
            {
                await notificationService.CreateNotificationAsync(
                    recipientId: rootUser.Id,
                    message: $"The project '{project.ProjectName}' has been marked completed by both parties.",
                    notificationType: "Project",
                    url: $"/projects/{project.ProjectId}" // Or wherever you route to project details
                );
            }
        }

        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            Message = "Your completion has been saved.",
            FullyCompleted = project.IsCompleted
        });
    }


}