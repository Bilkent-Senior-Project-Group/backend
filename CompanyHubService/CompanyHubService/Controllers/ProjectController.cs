using CompanyHubService.Data;
using CompanyHubService.DTOs;
using CompanyHubService.Models;
using CompanyHubService.Services;
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

    public ProjectController(CompanyService companyService, UserService userService, UserManager<User> userManager, CompanyHubDbContext dbContext)
    {
        this.companyService = companyService;
        this.userService = userService;
        this.userManager = userManager;
        this.dbContext = dbContext;
    }

    [HttpGet("GetProject/{projectId}")]
    public async Task<IActionResult> GetProject(Guid projectId)
    {
        var project = await dbContext.Projects
            .Where(p => p.ProjectId == projectId)
            .Include(p => p.Company) // ✅ Include the company that owns the project
            .FirstOrDefaultAsync();

        if (project == null)
        {
            return NotFound(new { Message = "Project not found." });
        }

        var projectDTO = new ProjectDTO
        {
            ProjectId = project.ProjectId,
            ProjectName = project.ProjectName,
            Description = project.Description,
            TechnologiesUsed = project.TechnologiesUsed?.Split(", ").ToList() ?? new List<string>(),
            Industry = project.Industry,
            ClientType = project.ClientType,
            Impact = project.Impact,
            Date = project.Date,
            ProjectUrl = project.ProjectUrl,
            Company = new CompanyDTO
            {
                CompanyId = project.Company.CompanyId,
                CompanyName = project.Company.CompanyName
            }
        };

        return Ok(projectDTO);
    }

}