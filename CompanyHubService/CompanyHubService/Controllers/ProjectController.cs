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

    // Here the root user selects his/her company as the client company.
    // The root user can select his/her company from a dropdown list of his/her companies.
    [HttpPost("CreateProjectRequest")]
    [Authorize(Roles = "Root")] // Only root user can create a project request.   
    public async Task<IActionResult> CreateProjectRequest([FromBody] ProjectRequestDTO request)
    {
        var result = await projectService.CreateProjectRequestAsync(request);
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

        var projectRequestDTO = new ProjectRequestDTO
        {
            ClientCompanyId = projectRequest.ClientCompanyId,
            ProviderCompanyId = projectRequest.ProviderCompanyId ?? Guid.Empty,
            ProjectName = projectRequest.ProjectName,
            Description = projectRequest.Description,
            TechnologiesUsed = projectRequest.TechnologiesUsed?.Split(", ").ToList() ?? new List<string>(),
            Industry = projectRequest.Industry,
            ClientType = projectRequest.ClientType,
            Impact = projectRequest.Impact
        };

        return Ok(projectRequestDTO);
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
}