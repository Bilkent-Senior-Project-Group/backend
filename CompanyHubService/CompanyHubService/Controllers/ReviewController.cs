using System.Security.Claims;
using CompanyHubService.Data;
using CompanyHubService.DTOs;
using CompanyHubService.Models;
using CompanyHubService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly CompanyHubDbContext _dbContext;


    public ReviewsController(CompanyHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost("PostReview")]
    [Authorize]
    public async Task<IActionResult> PostReview([FromBody] ReviewDTO reviewDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "User ID not found in token." });
        }

        var project = await _dbContext.Projects
        .Include(p => p.ProjectCompany)
        .ThenInclude(pc => pc.ProviderCompany)
        .Include(p => p.ProjectCompany.ClientCompany)
        .FirstOrDefaultAsync(p => p.ProjectId == reviewDto.ProjectId);

        if (project == null || !project.IsCompleted)
        {
            return BadRequest("Project not found or not completed yet.");
        }

        // Ensure the user is part of the client company
        var isClientUser = await _dbContext.UserCompanies.AnyAsync(uc =>
            uc.UserId == userId && uc.CompanyId == project.ProjectCompany.ClientCompanyId);

        if (!isClientUser)
        {
            return Forbid("Only users from the client company can submit a review.");
        }

        // Optional: prevent duplicate reviews by same user on the same project
        var existingReview = await _dbContext.Reviews.AnyAsync(r =>
            r.ProjectId == reviewDto.ProjectId && r.UserId == userId);

        if (existingReview)
        {
            return BadRequest("You have already reviewed this project.");
        }

        var review = new Review
        {
            ReviewId = Guid.NewGuid(),
            ReviewText = reviewDto.ReviewText,
            Rating = reviewDto.Rating,
            DatePosted = DateTime.UtcNow,
            ProjectId = reviewDto.ProjectId,
            UserId = userId
        };

        _dbContext.Reviews.Add(review);
        await _dbContext.SaveChangesAsync();

        await UpdateCompanyRating(project.ProjectCompany.ProviderCompanyId);

        // ✅ Create response DTO
        var reviewResponse = new ReviewResponseDTO
        {
            ReviewId = review.ReviewId,
            ReviewText = review.ReviewText,
            Rating = review.Rating,
            DatePosted = review.DatePosted,
            ProjectName = project.ProjectName,
            ProviderCompanyName = project.ProjectCompany.ProviderCompany?.CompanyName,
            UserName = (await _dbContext.Users.FindAsync(userId))?.UserName
        };

        return CreatedAtAction(nameof(GetReview), new { id = review.ReviewId }, reviewResponse);
    }



    [HttpGet("GetReview/{id}")]
    public async Task<IActionResult> GetReview(Guid id)
    {
        var review = await _dbContext.Reviews
        .Include(r => r.User)
        .Include(r => r.Project)
            .ThenInclude(p => p.ProjectCompany) // include ProjectCompany
        .ThenInclude(pc => pc.ProviderCompany) // then ProviderCompany from that
        .FirstOrDefaultAsync(r => r.ReviewId == id);


        if (review == null)
        {
            return NotFound();
        }

        var reviewDto = new ReviewResponseDTO
        {
            ReviewId = review.ReviewId,
            ReviewText = review.ReviewText,
            Rating = review.Rating,
            DatePosted = review.DatePosted,
            ProjectName = review.Project.ProjectName,
            ProviderCompanyName = review.Project.ProjectCompany?.ProviderCompany?.CompanyName,
            UserName = review.User?.UserName
        };

        return Ok(reviewDto);
    }


    [HttpGet("ByProject/{projectId}")]
    public async Task<IActionResult> GetReviewsForProject(Guid projectId)
    {
        var project = await _dbContext.Projects
            .Include(p => p.ProjectCompany)
                .ThenInclude(pc => pc.ProviderCompany)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);

        if (project == null)
            return NotFound(new { Message = "Project not found." });

        var reviews = await _dbContext.Reviews
            .Where(r => r.ProjectId == projectId)
            .Include(r => r.User)
            .Select(r => new ReviewResponseDTO
            {
                ReviewId = r.ReviewId,
                ReviewText = r.ReviewText,
                Rating = r.Rating,
                DatePosted = r.DatePosted,
                ProjectName = project.ProjectName,
                ProviderCompanyName = project.ProjectCompany.ProviderCompany.CompanyName,
                UserName = r.User.UserName
            })
            .ToListAsync();

        return Ok(reviews);
    }

    [HttpGet("GetReviewsByCompany/{companyId}")]
    public async Task<IActionResult> GetReviewsByCompany(Guid companyId)
    {
        var company = await _dbContext.Companies.FindAsync(companyId);
        if (company == null)
        {
            return NotFound(new { Message = "Company not found." });
        }

        var reviews = await _dbContext.Reviews
            .Include(r => r.Project)
                .ThenInclude(p => p.ProjectCompany)
                    .ThenInclude(pc => pc.ProviderCompany)
            .Where(r => r.Project.ProjectCompany.ProviderCompanyId == companyId)
            .Select(r => new ReviewResponseDTO
            {
                ReviewId = r.ReviewId,
                ReviewText = r.ReviewText,
                Rating = r.Rating,
                DatePosted = r.DatePosted,
                ProjectName = r.Project.ProjectName,
                ProviderCompanyName = r.Project.ProjectCompany.ProviderCompany.CompanyName,
                UserName = r.User.UserName
            })
            .ToListAsync();

        return Ok(reviews);
    }


    private async Task UpdateCompanyRating(Guid? companyId)
    {
        // Get all project IDs where the company is the provider
        var providerProjectIds = await _dbContext.ProjectCompanies
            .Where(pc => pc.ProviderCompanyId == companyId)
            .Select(pc => pc.ProjectId)
            .ToListAsync();

        if (providerProjectIds == null || !providerProjectIds.Any())
        {
            var company = await _dbContext.Companies.FindAsync(companyId);
            if (company != null)
            {
                company.OverallRating = 0;
                await _dbContext.SaveChangesAsync();
            }
            return;
        }

        // Get all reviews tied to those projects
        var ratings = await _dbContext.Reviews
            .Where(r => providerProjectIds.Contains(r.ProjectId))
            .Select(r => r.Rating)
            .ToListAsync();

        var companyToUpdate = await _dbContext.Companies.FindAsync(companyId);
        if (companyToUpdate == null)
            return;

        if (ratings.Any())
        {
            companyToUpdate.OverallRating = ratings.Average();
        }
        else
        {
            companyToUpdate.OverallRating = 0;
        }

        await _dbContext.SaveChangesAsync();
    }


}