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

        var company = await _dbContext.Companies.FindAsync(reviewDto.CompanyId);
        if (company == null)
        {
            return NotFound(new { Message = "Company not found." });
        }

        var review = new Review
        {
            ReviewId = Guid.NewGuid(),
            ReviewText = reviewDto.ReviewText,
            Rating = reviewDto.Rating,
            DatePosted = DateTime.UtcNow,
            CompanyId = reviewDto.CompanyId,
            UserId = userId
        };

        _dbContext.Reviews.Add(review);
        await _dbContext.SaveChangesAsync();

        await UpdateCompanyRating(review.CompanyId);

        // ✅ Create response DTO
        var reviewResponse = new ReviewResponseDTO
        {
            ReviewId = review.ReviewId,
            ReviewText = review.ReviewText,
            Rating = review.Rating,
            DatePosted = review.DatePosted,
            CompanyName = company.CompanyName,
            UserName = (await _dbContext.Users.FindAsync(userId))?.UserName
        };

        return CreatedAtAction(nameof(GetReview), new { id = review.ReviewId }, reviewResponse);
    }



    [HttpGet("GetReview/{id}")]
    public async Task<IActionResult> GetReview(Guid id)
    {
        var review = await _dbContext.Reviews
            .Include(r => r.Company)
            .Include(r => r.User)
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
            CompanyName = review.Company.CompanyName, // ✅ Get company name
            UserName = review.User.UserName // ✅ Get user name instead of ID
        };

        return Ok(reviewDto);
    }

    [HttpGet("ByCompany/{companyId}")]
    public async Task<IActionResult> GetReviewsForCompany(Guid companyId)
    {
        var reviews = await _dbContext.Reviews
            .Where(r => r.CompanyId == companyId)
            .Include(r => r.User)
            .Include(r => r.Company)
            .Select(r => new ReviewResponseDTO
            {
                ReviewId = r.ReviewId,
                ReviewText = r.ReviewText,
                Rating = r.Rating,
                DatePosted = r.DatePosted,
                CompanyName = r.Company.CompanyName, // ✅ Get company name
                UserName = r.User.UserName // ✅ Get user name instead of ID
            })
            .ToListAsync();

        return Ok(reviews);
    }

    private async Task UpdateCompanyRating(Guid companyId)
    {
        var company = await _dbContext.Companies
            .Include(c => c.Reviews)
            .FirstOrDefaultAsync(c => c.CompanyId == companyId);

        if (company != null)
        {
            var totalReviews = company.Reviews.Count;
            if (totalReviews > 0)
            {
                company.OverallRating = company.Reviews.Average(r => r.Rating);
            }
            else
            {
                company.OverallRating = 0; // Reset if there are no reviews
            }

            await _dbContext.SaveChangesAsync();
        }
    }

}