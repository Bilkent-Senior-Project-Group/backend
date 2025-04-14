

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CompanyHubService.Data;
using CompanyHubService.DTOs;
using CompanyHubService.Models;
using Microsoft.EntityFrameworkCore;


public class AnalyticsService 
{
    private readonly CompanyHubDbContext _context;

    public AnalyticsService(CompanyHubDbContext context)
    {
        _context = context;
    }

    public async Task InsertSearchQueryDataAsync(List<Guid> companyIds, string queryText)
    {
        var searchLog = new SearchQueryLog
        {
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
            .Where(sq => sq.CompanyIds.Contains(companyId))
            .Select(sq => new SearchQueryLogDTO
            {
                Id = sq.Id,
                VisitorId = sq.VisitorId,
                CompanyIds = sq.CompanyIds,
                QueryText = sq.QueryText,
                SearchDate = sq.SearchDate
            })
            .ToListAsync();

        return searchQueries;
    }

    public async Task<List<ProfileViewDTO>> GetVisitorCompaniesAsync(Guid companyId)
    {
        var profileViews = await _context.ProfileViews
            .Where(pv => pv.CompanyId == companyId && pv.ViewDate >= DateTime.UtcNow.AddDays(-30))
            .GroupBy(pv => new { pv.VisitorUserId, pv.CompanyId })
            .Select(g => g.OrderByDescending(pv => pv.ViewDate).FirstOrDefault())
            .Where(pv => pv != null)
            .OrderByDescending(pv => pv.ViewDate)
            .Select(pv => new ProfileViewDTO
            {
                Id = pv.Id,
                VisitorUserId = pv.VisitorUserId,
                CompanyId = pv.CompanyId,
                ViewDate = pv.ViewDate,
                FromWhere = pv.FromWhere
            })
            .ToListAsync();

        return profileViews;
    }
    
    public async Task<ProfileView> InsertProfileViewAsync(ProfileViewDTO profileViewDto)
    {
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