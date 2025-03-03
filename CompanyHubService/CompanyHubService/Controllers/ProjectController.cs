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

    [HttpGet("GetProject/{projectId}")]
    public async Task<IActionResult> GetProject(Guid projectId)
    {
        var projectDTO = await projectService.GetProjectAsync(projectId);
        if (projectDTO == null)
            return NotFound(new { Message = "Project not found." });

        return Ok(projectDTO);

    }

    [HttpPost("CreateProjectRequest")]
    [Authorize]
    public async Task<IActionResult> CreateProjectRequest([FromBody] ProjectRequestDTO request)
    {
        var result = await projectService.CreateProjectRequestAsync(request);
        if (result != "Project request sent successfully.")
            return BadRequest(new { Message = result });

        return Ok(new { Message = result });
    }

    [HttpGet("GetProjectRequest/{requestId}")]
    public async Task<IActionResult> GetProjectRequest(Guid requestId)
    {
        var projectRequest = await dbContext.ProjectRequests
            .Include(pr => pr.ClientCompany)
            .Include(pr => pr.ProviderCompany)
            .FirstOrDefaultAsync(pr => pr.RequestId == requestId);

        if (projectRequest == null)
        {
            return NotFound(new { Message = "Project request not found." });
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
    [Authorize]
    public async Task<IActionResult> ApproveProjectRequest(Guid requestId, [FromBody] bool isAccepted)
    {
        var result = await projectService.ApproveProjectRequestAsync(requestId, isAccepted);
        if (result.Contains("not found") || result.Contains("already been processed"))
            return BadRequest(new { Message = result });

        return Ok(new { Message = result });
    }




}