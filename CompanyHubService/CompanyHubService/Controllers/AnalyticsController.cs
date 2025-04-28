
using System.Security.Claims;
using System.Threading.Tasks;
using CompanyHubService.DTOs;
using Microsoft.AspNetCore.Mvc;

using CompanyHubService.Data;

using CompanyHubService.Models;
using CompanyHubService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using iText.Layout.Element;
[ApiController]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    
    private readonly AnalyticsService _analyticsService;
    public AnalyticsController(AnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

   

    [HttpGet("GetSearchQueries/{companyId}")]
    public async Task<IActionResult> GetSearchQueries(Guid companyId)
    {
        var searchQueries = await _analyticsService.GetSearchQueriesAsync(companyId);
        
        
        return Ok(searchQueries);
    }

    [HttpGet("GetProfileViews/{companyId}")]
    public async Task<IActionResult> GetProfileViews(Guid companyId)
    {
        var profileViews = await _analyticsService.GetVisitorCompaniesAsync(companyId);
        
        return Ok(profileViews);
    }
    [HttpPost("InsertSearchQueryData")]
    [Authorize]
    public async Task<IActionResult> InsertSearchQueryData([FromBody] SearchQueryLogWrapperDTO wrapper)
    {
        if (wrapper?.searchQueryLogDto == null)
            return BadRequest(new { Message = "Invalid data." });

        var searchQueryLogDto = wrapper.searchQueryLogDto;
        Console.WriteLine("InsertSearchQueryData called with data: " + searchQueryLogDto.ToString());
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
        {
            searchQueryLogDto.VisitorId = userId;
        }
        else
        {
            // Handle the case where the user is not authenticated
            return Unauthorized(new { Message = "User is not authenticated." });
        }

        // The VisitorId will be handled in the service layer using HttpContextAccessor
        await _analyticsService.InsertSearchQueryDataAsync(searchQueryLogDto);

        return Ok(new { Message = "Search query data inserted successfully." });
    }
    [HttpPost("InsertProfileViewData")]
    public async Task<IActionResult> InsertProfileViewData([FromBody] ProfileViewDTO profileViewDTO)
    {
        if (profileViewDTO == null)
            return BadRequest(new { Message = "Invalid data." });
        

        await _analyticsService.InsertProfileViewAsync(profileViewDTO);
        return Ok(new { Message = "Profile view data inserted successfully." });
    }

}



