using CompanyHubService.Data;
using CompanyHubService.DTOs;
using CompanyHubService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Confluent.Kafka;
using System.Text.Json;
using Microsoft.VisualBasic;

namespace CompanyHubService.Services
{
    public class ProjectService
    {
        private CompanyHubDbContext dbContext { get; set; }
        private UserService userService { get; set; }
        private NotificationService notificationService { get; set; }
        private readonly IProducer<string, string> _kafkaProducer;


        public ProjectService(CompanyHubDbContext dbContext, UserService userService, NotificationService notificationService, IProducer<string, string> kafkaProducer)
        {
            this.dbContext = dbContext;
            this.userService = userService;
            this.notificationService = notificationService;
            this._kafkaProducer = kafkaProducer;
        }

        public async Task<ProjectViewDTO?> GetProjectAsync(Guid projectId)
        {
            var project = await dbContext.Projects
                .Where(p => p.ProjectId == projectId)
                .Include(p => p.ProjectCompany)
                    .ThenInclude(pc => pc.ClientCompany)
                .Include(p => p.ProjectCompany)
                    .ThenInclude(pc => pc.ProviderCompany)
                .Include(p => p.ServiceProjects) // Include ServiceProject relationships
                    .ThenInclude(sp => sp.Service) // Include the related services
                .FirstOrDefaultAsync();

            if (project == null) return null;

            // Extract the services related to this project from the ServiceProjects collection
            var services = project.ServiceProjects
                .Select(sp => sp.Service) // Access the Service navigation property
                .ToList();

            return new ProjectViewDTO
            {
                ProjectId = project.ProjectId,
                ProjectName = project.ProjectName,
                Description = project.Description,
                TechnologiesUsed = project.TechnologiesUsed?.Split(", ").ToList() ?? new List<string>(),
                ClientType = project.ClientType,
                StartDate = project.StartDate,
                CompletionDate = project.CompletionDate,
                IsOnCompedia = project.IsOnCompedia,
                IsCompleted = project.IsCompleted,
                ProjectUrl = project.ProjectUrl,
                ClientCompanyName = project.ProjectCompany.IsClient == 0 ? project.ProjectCompany.OtherCompanyName : project.ProjectCompany.IsClient == 1 ? project.ProjectCompany.ClientCompany.CompanyName : project.ProjectCompany.ClientCompany.CompanyName,
                ProviderCompanyName = project.ProjectCompany.IsClient == 0 ? project.ProjectCompany.ProviderCompany.CompanyName : project.ProjectCompany.IsClient == 1 ? project.ProjectCompany.OtherCompanyName : project.ProjectCompany.ProviderCompany.CompanyName,
                Services = services.Select(s => new ServiceDTO
                {
                    Id = s.Id,
                    Name = s.Name,
                    
                }).ToList()
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
                ClientType = request.ClientType,
                Services = request.Services,
                Impact = request.Impact,
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
                ClientType = projectRequest.ClientType,
                StartDate = DateTime.UtcNow,
                IsOnCompedia = true,
                ProjectUrl = "https://default.url",
            };

            var projectServices = new List<ServiceProject> { }; // List to hold project-service mappings

            // Add Project-Service mappings
            if (projectRequest.Services != null && projectRequest.Services.Any())
            {
                var projectServiceMappings = projectRequest.Services.Select(service => new ServiceProject
                {
                    ProjectId = newProject.ProjectId,
                    ServiceId = service,
                }).ToList();

                projectServices.AddRange(projectServiceMappings);
            }

            // Add the project-service mappings to the DbContext in a single operation
            if (projectServices.Any())
            {
                dbContext.ServiceProjects.AddRange(projectServices);
            }

            var projectCompany = new ProjectCompany
            {
                ProjectId = newProject.ProjectId,
                ClientCompanyId = projectRequest.ClientCompanyId,
                ProviderCompanyId = projectRequest.ProviderCompanyId
            };

            dbContext.Projects.Add(newProject);
            dbContext.ProjectCompanies.Add(projectCompany);
            await dbContext.SaveChangesAsync();

            // Send Kafka message for provider company update
            try
            {
                // Retrieve the provider company with related entities
                var providerCompany = await dbContext.Companies
                    .Include(c => c.ServiceCompanies)
                    .Include(c => c.Projects)
                    .Include(c => c.Products)
                    .Include(c => c.Reviews)
                    .FirstOrDefaultAsync(c => c.CompanyId == projectRequest.ProviderCompanyId);

                if (providerCompany != null)
                {
                    // Create the DTO with only the fields we want to send
                    var companyKafkaDTO = new CompanyKafkaDTO
                    {
                        CompanyId = providerCompany.CompanyId,
                        Description = providerCompany.Description,
                        FoundedYear = providerCompany.FoundedYear,
                        CompanySize = providerCompany.CompanySize,
                        Location = providerCompany.Location,
                        OverallRating = providerCompany.OverallRating,
                        ServiceCompanies = providerCompany.ServiceCompanies,
                        Projects = providerCompany.Projects,
                        Products = providerCompany.Products,
                        Reviews = providerCompany.Reviews
                    };

                    // Serialize the DTO to JSON
                    var messageValue = System.Text.Json.JsonSerializer.Serialize(companyKafkaDTO);

                    // Create the Kafka message
                    var message = new Message<string, string>
                    {
                        Key = providerCompany.CompanyId.ToString(),
                        Value = messageValue
                    };

                    var result = await _kafkaProducer.ProduceAsync("modifyCompany", message);
                    Console.WriteLine($"Sent company update to Kafka topic {result.TopicPartitionOffset}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error producing Kafka message: {ex.Message}");
                // Continue execution as the main operation was successful
            }

            return "Project request approved and project created successfully.";
        }

    }


}

