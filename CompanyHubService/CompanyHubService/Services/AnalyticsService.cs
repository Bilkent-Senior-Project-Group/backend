

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using CompanyHubService.Data;
using CompanyHubService.DTOs;
using CompanyHubService.Models;
using CompanyHubService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
public class AnalyticsService 
{
    private readonly CompanyHubDbContext _context;
    private readonly UserService _userService;
    private readonly AuthService _authService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public AnalyticsService(CompanyHubDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }



    public async Task InsertSearchQueryDataAsync(List<Guid> companyIds, string queryText, string userId)
    {
        // Check if the user is authenticated
        if (userId == null)
        {
            await LogSearchQueryAsync(companyIds, queryText, null);
            return;
        }
        else {
            await LogSearchQueryAsync(companyIds, queryText, userId);
            
        }
        // Log the search query
        
    }
    private async Task LogSearchQueryAsync(List<Guid> companyIds, string queryText, string userId)
    {
        var searchLog = new SearchQueryLog
        {
            VisitorId = userId,
            CompanyIds = companyIds,
            QueryText = queryText,
            SearchDate = DateTime.UtcNow
        };
        _context.SearchQueryLogs.Add(searchLog);
        await _context.SaveChangesAsync();
    }
    public async Task<List<SearchQueryLogDTO>> GetSearchQueriesAsync(Guid companyId)
    {
       var searchQueries = await _context.SearchQueryLogs
            .ToListAsync();

       return searchQueries
            .Where(sq => sq.CompanyIds.Contains(companyId))
            .Select(sq => new SearchQueryLogDTO
            {
                Id = sq.Id,
                VisitorId = sq.VisitorId,
                CompanyIds = sq.CompanyIds,
                QueryText = sq.QueryText,
                SearchDate = sq.SearchDate
            })
            .ToList();
    }

    public async Task<List<ProfileViewDTO>> GetVisitorCompaniesAsync(Guid companyId)
    {
        // Get all user IDs of people who work at the company
        var companyUserIds = await _context.UserCompanies
            .Where(ucr => ucr.CompanyId == companyId && ucr.UserId != null)
            .Select(ucr => ucr.UserId)
            .ToListAsync();

        // Get profile views from the last 30 days
        var profileViews = await _context.ProfileViews
            .Where(pv => pv.CompanyId == companyId && pv.ViewDate >= DateTime.UtcNow.AddDays(-30))
            .ToListAsync();
            
        // Filter out visitors who work at the company
        var externalVisitors = profileViews
            .Where(pv => !string.IsNullOrEmpty(pv.VisitorUserId) && 
                        !companyUserIds.Contains(pv.VisitorUserId))
            .OrderByDescending(pv => pv.ViewDate)
            .Select(pv => new ProfileViewDTO
            {
                Id = pv.Id,
                VisitorUserId = pv.VisitorUserId,
                CompanyId = pv.CompanyId,
                ViewDate = pv.ViewDate,
                FromWhere = pv.FromWhere
            })
            .ToList();  

        // if there are no profileviews just return empty list
        if (externalVisitors == null || externalVisitors.Count == 0)
            return new List<ProfileViewDTO>();

        return externalVisitors;  
    }  
    public async Task<ProfileView> InsertProfileViewAsync(ProfileViewDTO profileViewDto)
    {
        // Get all user IDs of people who work at the company
        var companyUserIds = await _context.UserCompanies
            .Where(ucr => ucr.CompanyId == profileViewDto.CompanyId && ucr.UserId != null)
            .Select(ucr => ucr.UserId)
            .ToListAsync();
        // Check if the visitor is an employee of the company
        // Log the company user IDs for debugging


        // Console.WriteLine($"Company User IDs count: {companyUserIds.Count}");
        // if (companyUserIds != null && companyUserIds.Count > 0)
        // {
        //     Console.WriteLine($"Company User IDs: {string.Join(", ", companyUserIds)}");
        // }
        // else 
        // {
        //     Console.WriteLine("No company user IDs found");
        // }
        // Console.WriteLine($"Visitor User ID: {profileViewDto.VisitorUserId}");  // Check if the visitor is an employee of the company
        if (companyUserIds.Contains(profileViewDto.VisitorUserId))
        {
            // Console.WriteLine("Visitor is an employee of the company. Not inserting profile view.");
            return null;
        }
        // If the visitor is not an employee, insert the profile view
        var profileView = new ProfileView
        {
            VisitorUserId = profileViewDto.VisitorUserId,
            CompanyId = profileViewDto.CompanyId,
            ViewDate = profileViewDto.ViewDate,
            FromWhere = profileViewDto.FromWhere
        };
        
        _context.ProfileViews.Add(profileView);
        await _context.SaveChangesAsync();
        return profileView;
    }
    
}