
using System.Threading.Tasks;
using CompanyHubService.DTOs;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<IActionResult> InsertSearchQueryData([FromBody] SearchQueryLogDTO searchQueryLogDto, 
        [FromQuery] string visitorId)
    {
        if (searchQueryLogDto == null)
            return BadRequest(new { Message = "Invalid data." });
        

        await _analyticsService.InsertSearchQueryDataAsync(searchQueryLogDto.CompanyIds, searchQueryLogDto.QueryText, visitorId);
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



