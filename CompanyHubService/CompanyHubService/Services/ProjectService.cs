using CompanyHubService.Data;
using CompanyHubService.DTOs;
using CompanyHubService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Confluent.Kafka;
using System.Text.Json;

namespace CompanyHubService.Services
{
    public class ProjectService
    {
        private CompanyHubDbContext dbContext { get; set; }
        private UserService userService { get; set; }

        private NotificationService notificationService { get; set; }


        public ProjectService(CompanyHubDbContext dbContext, UserService userService, NotificationService notificationService)
        {
            this.dbContext = dbContext;
            this.userService = userService;
            this.notificationService = notificationService;
        }

        public async Task<ProjectDTO?> GetProjectAsync(Guid projectId)
        {
            var project = await dbContext.Projects
                .Where(p => p.ProjectId == projectId)
                .Include(p => p.ProjectCompany)
                .ThenInclude(pc => pc.ClientCompany)
                .Include(p => p.ProjectCompany)
                .ThenInclude(pc => pc.ProviderCompany)
                .FirstOrDefaultAsync();

            if (project == null) return null;

            return new ProjectDTO
            {
                ProjectId = project.ProjectId,
                ProjectName = project.ProjectName,
                Description = project.Description,
                TechnologiesUsed = project.TechnologiesUsed?.Split(", ").ToList() ?? new List<string>(),
                Industry = project.Industry,
                ClientType = project.ClientType,
                Impact = project.Impact,
                StartDate = project.StartDate,
                CompletionDate = project.CompletionDate,
                IsOnCompedia = project.IsOnCompedia,
                IsCompleted = project.IsCompleted,
                ProjectUrl = project.ProjectUrl,
                ClientCompanyName = project.ProjectCompany.ClientCompany.CompanyName,
                ProviderCompanyName = project.ProjectCompany.ProviderCompany.CompanyName
            };
        }

        public async Task<string> CreateProjectRequestAsync(ProjectRequestDTO request)
        {
            var clientCompany = await dbContext.Companies.FindAsync(request.ClientCompanyId);
            var providerCompany = await dbContext.Companies.FindAsync(request.ProviderCompanyId);

            if (clientCompany == null || providerCompany == null)
                return "Invalid client or provider company.";

            if (!providerCompany.Verified)
                return "The selected provider company is not verified.";

            if (!clientCompany.Verified)
                return "The selected client company is not verified.";

            var projectRequest = new ProjectRequest
            {
                ClientCompanyId = request.ClientCompanyId,
                ProviderCompanyId = request.ProviderCompanyId,
                ProjectName = request.ProjectName,
                Description = request.Description,
                TechnologiesUsed = request.TechnologiesUsed != null ? string.Join(", ", request.TechnologiesUsed) : "",
                Industry = request.Industry,
                ClientType = request.ClientType,
                Impact = request.Impact
            };

            dbContext.ProjectRequests.Add(projectRequest);
            await dbContext.SaveChangesAsync();

            // Fetch all users enrolled in the provider company
            var providerCompanyUsers = await dbContext.UserCompanies
                .Where(uc => uc.CompanyId == providerCompany.CompanyId)
                .Select(uc => uc.UserId) // Get only User IDs
                .ToListAsync();

            // Send a notification to each user in the provider company
            foreach (var userId in providerCompanyUsers)
            {
                await notificationService.CreateNotificationAsync(
                    recipientId: userId.ToString(),
                    notificationType: "Company",
                    message: $"Your company has received a project request from {clientCompany.CompanyName}.",
                    url: $"/project-requests/{projectRequest.RequestId}"
                );
            }


            return "Project request sent successfully.";
        }

        public async Task<string> ApproveProjectRequestAsync(Guid requestId, bool isAccepted)
        {
            var projectRequest = await dbContext.ProjectRequests.FindAsync(requestId);

            if (projectRequest == null)
                return "Project request not found.";

            if (projectRequest.IsAccepted || projectRequest.IsRejected)
                return "Project request has already been processed.";

            if (!isAccepted)
            {
                projectRequest.IsRejected = true;
                await dbContext.SaveChangesAsync();
                return "Project request rejected.";
            }

            projectRequest.IsAccepted = true;
            projectRequest.AcceptedDate = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();

            var newProject = new Project
            {
                ProjectId = Guid.NewGuid(),
                ProjectName = projectRequest.ProjectName,
                Description = projectRequest.Description,
                TechnologiesUsed = projectRequest.TechnologiesUsed,
                Industry = projectRequest.Industry,
                ClientType = projectRequest.ClientType,
                Impact = projectRequest.Impact,
                StartDate = DateTime.UtcNow,
                IsOnCompedia = true,
                ProjectUrl = "https://default.url"
            };

            var projectCompany = new ProjectCompany
            {
                ProjectId = newProject.ProjectId,
                ClientCompanyId = projectRequest.ClientCompanyId,
                ProviderCompanyId = projectRequest.ProviderCompanyId
            };

            dbContext.Projects.Add(newProject);
            dbContext.ProjectCompanies.Add(projectCompany);
            await dbContext.SaveChangesAsync();

            return "Project request approved and project created successfully.";
        }

    }


}

