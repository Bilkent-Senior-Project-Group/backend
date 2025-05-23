﻿using CompanyHubService.Data;
using CompanyHubService.DTOs;
using CompanyHubService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Confluent.Kafka;
using System.Text.Json;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using Newtonsoft.Json.Linq;
using iText.Commons.Actions.Contexts;
using iText.Kernel.Colors;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;

namespace CompanyHubService.Services
{
    public class CompanyService
    {
        private CompanyHubDbContext _dbContext { get; set; }
        private UserService userService { get; set; }
        private readonly IProducer<string, string> _kafkaProducer;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AnalyticsService analyticsService;
        private readonly UserManager<User> userManager;

        public CompanyService(CompanyHubDbContext dbContext, UserService userService, IProducer<string, string> kafkaProducer, IHttpClientFactory httpClientFactory, AnalyticsService analyticsService, UserManager<User> userManager)
        {
            _dbContext = dbContext;
            this.userService = userService;
            _kafkaProducer = kafkaProducer;
            _httpClientFactory = httpClientFactory;
            this.analyticsService = analyticsService;
            this.userManager = userManager;
        }

        public async Task<bool> CreateCompanyAsync(CreateCompanyRequestDTO request, string userId)
        {
            var company = new Company
            {
                CompanyId = Guid.NewGuid(),
                CompanyName = request.CompanyName,
                Description = request.Description,
                FoundedYear = request.FoundedYear,
                Location = request.Location,
                Address = request.Address,
                Website = request.Website,
                CompanySize = request.CompanySize,
                Verified = false,
                Phone = request.Phone,
                Email = request.Email,
                LogoUrl = "https://azurelogo.blob.core.windows.net/company-logos/defaultcompany.png",
                AddedOnPage = request.AddedOnPage,
                LastUpdated = DateTime.UtcNow,
            };

            var serviceCompanies = new List<ServiceCompany>();

            if (request.Services != null && request.Services.Any())
            {
                var serviceCount = request.Services.Count;
                var basePercentage = 100 / serviceCount;
                var remainder = 100 % serviceCount;

                serviceCompanies = request.Services
                    .Select((item, index) => new ServiceCompany
                    {
                        CompanyId = company.CompanyId,
                        ServiceId = item,
                        Percentage = basePercentage + (index < remainder ? 1 : 0)
                    })
                    .ToList();
            }

            // Add the company and service mappings to the DbContext
            _dbContext.Companies.Add(company);

            await _dbContext.SaveChangesAsync();

            // Add the project-service mappings to the DbContext in a single operation
            if (serviceCompanies.Any())
            {
                _dbContext.ServiceCompanies.AddRange(serviceCompanies);
            }

            // Create UserCompany mapping
            var userCompany = new UserCompany
            {
                UserId = userId,
                CompanyId = company.CompanyId,
            };
            _dbContext.UserCompanies.Add(userCompany);

            var user = await userManager.FindByIdAsync(userId);

            var isVerifiedUser = await userManager.IsInRoleAsync(user, "VerifiedUser");
            var isAlreadyRoot = await userManager.IsInRoleAsync(user, "Root");


            if (isVerifiedUser && !isAlreadyRoot)
            {
                await userManager.RemoveFromRoleAsync(user, "VerifiedUser");
                await userManager.AddToRoleAsync(user, "Root");
            }

            List<Project> projects = new List<Project>();

            if (request.Portfolio != null && request.Portfolio.Any())
            {
                var projectCompanies = new List<ProjectCompany>();
                var projectServices = new List<ServiceProject>(); // List to hold project-service mappings

                foreach (var p in request.Portfolio)
                {
                    var clientCompany = await _dbContext.Companies
                        .FirstOrDefaultAsync(c => c.CompanyName == p.ClientCompanyName);

                    var providerCompany = await _dbContext.Companies
                        .FirstOrDefaultAsync(c => c.CompanyName == p.ProviderCompanyName);


                    // If either client or provider company is not found, equalize client to provider
                    var flag = 0;
                    var IsClient = 2; // 0: provider, 1: client, 2: both found

                    if (clientCompany == null && providerCompany != null)
                    {
                        clientCompany = providerCompany;
                        flag = 1;
                        IsClient = 0;

                    }
                    else if (providerCompany == null && clientCompany != null)
                    {
                        providerCompany = clientCompany;
                        flag = 2;
                        IsClient = 1;
                    }

                    var newProject = new Project
                    {
                        ProjectId = Guid.NewGuid(),
                        ProjectName = p.ProjectName,
                        Description = p.Description,
                        TechnologiesUsed = string.Join(", ", p.TechnologiesUsed),
                        ClientType = p.ClientType,
                        StartDate = p.StartDate,
                        CompletionDate = p.CompletionDate,
                        IsOnCompedia = false,
                        IsCompleted = p.IsCompleted,
                        ProjectUrl = p.ProjectUrl
                    };

                    projects.Add(newProject);

                    // Create project-company mapping
                    var newProjectCompany = new ProjectCompany
                    {
                        ProjectId = newProject.ProjectId,
                        ClientCompanyId = flag == 0 ? clientCompany.CompanyId : flag == 1 ? providerCompany.CompanyId : clientCompany.CompanyId,
                        ProviderCompanyId = flag == 0 ? providerCompany.CompanyId : flag == 1 ? clientCompany.CompanyId : providerCompany.CompanyId,
                        OtherCompanyName = flag == 0 ? null : flag == 1 ? p.ClientCompanyName : p.ProviderCompanyName,
                        IsClient = IsClient
                    };

                    projectCompanies.Add(newProjectCompany);

                    // Add Project-Service mappings
                    if (p.Services != null && p.Services.Any())
                    {
                        var projectServiceMappings = p.Services.Select(serviceId => new ServiceProject
                        {
                            ProjectId = newProject.ProjectId,
                            ServiceId = serviceId
                        }).ToList();

                        projectServices.AddRange(projectServiceMappings);
                    }
                }

                // Add all projects, project-company mappings, and project-service mappings
                _dbContext.Projects.AddRange(projects);
                _dbContext.ProjectCompanies.AddRange(projectCompanies);

                // Add the project-service mappings to the DbContext in a single operation
                if (projectServices.Any())
                {
                    _dbContext.ServiceProjects.AddRange(projectServices);
                }
            }

            // Save all changes in a single transaction
            await _dbContext.SaveChangesAsync();

            try
            {
                // Create a list of service IDs and names
                var serviceData = new List<object>();
                foreach (var sc in serviceCompanies)
                {
                    // Get the service name from the service ID
                    var service = await _dbContext.Services.FindAsync(sc.ServiceId);
                    serviceData.Add(new
                    {
                        ServiceId = sc.ServiceId,
                        ServiceName = service?.Name // Include null check
                    });
                }

                // Create a list of simplified project data
                var projectData = projects.Select(p => new
                {
                    ProjectId = p.ProjectId,
                    ProjectName = p.ProjectName,
                    Description = p.Description,
                    TechnologiesUsed = p.TechnologiesUsed,
                    ClientType = p.ClientType,
                    StartDate = p.StartDate,
                    CompletionDate = p.CompletionDate,
                    IsOnCompedia = p.IsOnCompedia,
                    IsCompleted = p.IsCompleted,
                    ProjectUrl = p.ProjectUrl
                    // Navigation properties are explicitly excluded
                }).ToList();

                var locationInfo = await _dbContext.CitiesAndCountries
                    .Where(x => x.ID == company.Location)
                    .Select(x => new { x.City, x.Country })
                    .FirstOrDefaultAsync();

                var locationName = locationInfo != null ? $"{locationInfo.City}, {locationInfo.Country}" : null;


                var companyKafkaDTO = new
                {
                    CompanyId = company.CompanyId,
                    CompanyName = company.CompanyName,
                    Description = company.Description,
                    FoundedYear = company.FoundedYear,
                    CompanySize = company.CompanySize,
                    Location = company.Location,
                    LocationName = locationName,
                    OverallRating = company.OverallRating,
                    Services = serviceData,
                    Projects = projectData,
                    Products = new List<object>(), // Empty as new company
                    Reviews = new List<object>() // Empty as new company
                };

                // Configure serialization options
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true, // For easier debugging
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase // Optional: for JSON naming conventions
                };

                // Serialize the DTO to JSON
                var messageValue = System.Text.Json.JsonSerializer.Serialize(companyKafkaDTO, options);

                // Create the Kafka message
                var message = new Message<string, string>
                {
                    Key = company.CompanyId.ToString(),
                    Value = messageValue
                };

                var result = await _kafkaProducer.ProduceAsync("createCompany", message);
                Console.WriteLine($"Sent message to Kafka topic {result.TopicPartitionOffset}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error producing Kafka message: {ex.Message}");
                // Don't return false here as company was already created successfully
            }

            return true;
        }


        public async Task<bool> ModifyCompanyProfileAsync(CompanyProfileModifyDTO companyProfileDTO)
        {
            try
            {
                // Retrieve the company with related entities from the database
                var company = await _dbContext.Companies
                    .Include(c => c.ServiceCompanies)
                    .Include(c => c.Projects)
                    .Include(c => c.Products)
                    .Include(c => c.Reviews)
                    .FirstOrDefaultAsync(c => c.CompanyId == companyProfileDTO.CompanyId);

                if (company == null)
                {
                    Console.WriteLine($"Company with ID {companyProfileDTO.CompanyId} not found");
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(companyProfileDTO.Name))
                {
                    var isNameTaken = await _dbContext.Companies
                        .AnyAsync(c => c.CompanyName == companyProfileDTO.Name && c.CompanyId != company.CompanyId);

                    if (isNameTaken)
                    {
                        Console.WriteLine($"Company name '{companyProfileDTO.Name}' is already taken by another company.");
                        return false; // Return false or handle the error as needed
                    }
                    company.CompanyName = companyProfileDTO.Name;
                }


                if (!string.IsNullOrWhiteSpace(companyProfileDTO.Description))
                    company.Description = companyProfileDTO.Description;

                if (companyProfileDTO.FoundedYear != 0)
                    company.FoundedYear = companyProfileDTO.FoundedYear;

                if (!string.IsNullOrWhiteSpace(companyProfileDTO.CompanySize))
                    company.CompanySize = companyProfileDTO.CompanySize;

                if (companyProfileDTO.Location != 0)
                    company.Location = companyProfileDTO.Location;

                if (!string.IsNullOrWhiteSpace(companyProfileDTO.Phone))
                    company.Phone = companyProfileDTO.Phone;

                if (!string.IsNullOrWhiteSpace(companyProfileDTO.Email))
                    company.Email = companyProfileDTO.Email;

                if (!string.IsNullOrWhiteSpace(companyProfileDTO.Address))
                    company.Address = companyProfileDTO.Address;

                if (!string.IsNullOrWhiteSpace(companyProfileDTO.Website))
                    company.Website = companyProfileDTO.Website;

                company.LastUpdated = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                if (companyProfileDTO.Services != null && companyProfileDTO.Services.Any())
                {
                    // 1. Remove all existing service mappings for this company
                    var oldMappings = await _dbContext.ServiceCompanies
                        .Where(sc => sc.CompanyId == company.CompanyId)
                        .ToListAsync();

                    _dbContext.ServiceCompanies.RemoveRange(oldMappings);

                    // 2. Add new service mappings
                    var newMappings = companyProfileDTO.Services.Select(dto => new ServiceCompany
                    {
                        CompanyId = company.CompanyId,
                        ServiceId = dto.Id,
                        Percentage = dto.Percentage
                    }).ToList();

                    await _dbContext.ServiceCompanies.AddRangeAsync(newMappings);

                    // 3. Save all changes
                    await _dbContext.SaveChangesAsync();
                }

                // Create the DTO with only the fields we want to send
                var companyKafkaDTO = new CompanyKafkaDTO
                {
                    CompanyId = company.CompanyId,
                    Description = company.Description,
                    FoundedYear = company.FoundedYear,
                    CompanySize = company.CompanySize,
                    Location = company.Location,
                    OverallRating = company.OverallRating,
                    ServiceCompanies = company.ServiceCompanies,
                    Projects = company.Projects,
                    Products = company.Products,
                    Reviews = company.Reviews
                };

                // Serialize the DTO to JSON
                var messageValue = System.Text.Json.JsonSerializer.Serialize(
                companyKafkaDTO,
                new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles,
                    WriteIndented = false
                });


                // Create the Kafka message
                var message = new Message<string, string>
                {
                    Key = companyProfileDTO.CompanyId.ToString(), // Use CompanyId as the key
                    Value = messageValue                          // JSON payload as the value
                };

                var result = await _kafkaProducer.ProduceAsync("modifyCompany", message);
                Console.WriteLine($"Sent message to Kafka topic {result.TopicPartitionOffset}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error producing Kafka message: {ex.Message}");
                return false;
            }

            return true;
        }

        public async Task<List<UserDTO>> GetUsersOfCompanyAsync(Guid companyId)
        {
            var users = await _dbContext.UserCompanies
                .Where(uc => uc.CompanyId == companyId)
                .Include(uc => uc.User) // Include User details
                .Select(uc => new UserDTO
                {
                    Id = uc.User.Id,
                    FirstName = uc.User.FirstName,
                    LastName = uc.User.LastName,
                    UserName = uc.User.UserName,
                    Email = uc.User.Email,
                    PhoneNumber = uc.User.PhoneNumber,
                })
                .ToListAsync();

            return users;
        }

        public async Task<List<UserCompanyWithTechnologiesDTO>> GetCompaniesOfUserAsync(string userId)
        {
            // 1. Get the user's company IDs
            var userCompanyIds = await _dbContext.UserCompanies
                .Where(uc => uc.UserId == userId)
                .Select(uc => uc.CompanyId)
                .ToListAsync();

            // 2. Get company info with services
            var companies = await _dbContext.Companies
                .Where(c => userCompanyIds.Contains(c.CompanyId))
                .Include(c => c.ServiceCompanies)
                    .ThenInclude(sc => sc.Service)
                .ToListAsync();

            // 3. Get related projects through ProjectCompanies
            var projectCompanies = await _dbContext.ProjectCompanies
                .Where(pc =>
                    (pc.ClientCompanyId.HasValue && userCompanyIds.Contains(pc.ClientCompanyId.Value)) ||
                    (pc.ProviderCompanyId.HasValue && userCompanyIds.Contains(pc.ProviderCompanyId.Value)))
                .Include(pc => pc.Project)
                .ToListAsync();

            // 4. Compose the final result
            var result = companies.Select(company =>
            {
                var relatedProjects = projectCompanies
                    .Where(pc =>
                        pc.ClientCompanyId == company.CompanyId ||
                        (pc.ProviderCompanyId.HasValue && pc.ProviderCompanyId.Value == company.CompanyId))
                    .Select(pc => pc.Project)
                    .Where(p => p != null && !string.IsNullOrWhiteSpace(p.TechnologiesUsed))
                    .ToList();

                var technologies = relatedProjects
                    .SelectMany(p => p.TechnologiesUsed.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    .Select(t => t.Trim())
                    .Distinct()
                    .ToList();

                return new UserCompanyWithTechnologiesDTO
                {
                    CompanyId = company.CompanyId,
                    CompanyName = company.CompanyName,
                    Services = company.ServiceCompanies.Select(sc => sc.Service.Name).ToList(),
                    TechnologiesUsed = technologies
                };
            }).ToList();

            return result;
        }


        public async Task<string> FreeTextSearchAsync(FreeTextSearchDTO searchQuery, string? userId)
        {
            string fastApiUrl = "http://127.0.0.1:8000/search";

            // Create the payload with optional filters
            var payload = new
            {
                query = searchQuery.searchQuery,
                locations = searchQuery.locations,
                service_ids = searchQuery.serviceIds?.Select(id => id.ToString()).ToList()
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                var client = _httpClientFactory.CreateClient();
                HttpResponseMessage response = await client.PostAsync(fastApiUrl, content);
                response.EnsureSuccessStatusCode();

                // Deserialize the FastAPI response
                var responseBody = await response.Content.ReadAsStringAsync();

                // **DEBUG LOG: Print raw API response**
                Console.WriteLine("DEBUG: Received Search Results -> " + responseBody);

                var searchResults = JsonConvert.DeserializeObject<SearchResponseDTO>(responseBody);

                // Extract company IDs
                var companyIds = searchResults.Results
                    .Select(r => Guid.TryParse(r.CompanyId, out var guid) ? guid : Guid.Empty)
                    .Where(guid => guid != Guid.Empty)  // Remove failed conversions
                    .ToList();

                // Fetch company details from the SQL database
                var companies = await _dbContext.Companies
                    .Where(c => companyIds.Contains(c.CompanyId))
                    .Join(_dbContext.CitiesAndCountries,
                          c => c.Location,
                          loc => loc.ID,
                          (c, loc) => new
                          {
                              c.CompanyId,
                              Name = c.CompanyName,
                              Size = c.CompanySize.ToString(),
                              Location = loc.City + " " + loc.Country,
                              c.Description
                          })
                    .ToListAsync();

                // Step 2: Fetch service names per company
                var serviceMap = await _dbContext.ServiceCompanies
                    .Where(sc => companyIds.Contains(sc.CompanyId))
                    .Include(sc => sc.Service)
                    .GroupBy(sc => sc.CompanyId)
                    .ToDictionaryAsync(
                        g => g.Key,
                        g => g.Select(sc => sc.Service.Name).ToList()
                    );

                // Step 3: Merge search results with service names
                var enrichedResults = searchResults.Results
                .Join(companies,
                      r => Guid.TryParse(r.CompanyId, out var guid) ? guid : Guid.Empty,
                      c => c.CompanyId,
                      (r, c) => new
                      {
                          c.CompanyId,
                          c.Name,
                          c.Size,
                          Location = c.Location,
                          c.Description,
                          Services = serviceMap.ContainsKey(c.CompanyId) ? serviceMap[c.CompanyId] : new List<string>(),
                          r.Distance
                      })
                .ToList();


                // await analyticsService.InsertSearchQueryDataAsync(companyIds, searchQuery.searchQuery, userId);

                return JsonConvert.SerializeObject(new
                {
                    query = searchResults.Query,
                    extracted = searchResults.Extracted,
                    appliedFilters = new
                    {
                        locations = searchQuery.locations,
                        serviceIds = searchQuery.serviceIds
                    },
                    results = enrichedResults
                });
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Failed to process search",
                    details = ex.Message
                });
            }
        }

        // DTO for parsing FastAPI response
        public class SearchResponseDTO
        {
            public string Query { get; set; }
            public ExtractedFieldsDTO Extracted { get; set; }
            public List<SearchResultDTO> Results { get; set; }
        }

        public class ExtractedFieldsDTO
        {
            public List<string> Expertise { get; set; }
            public List<string> TechnologiesUsed { get; set; }
        }

        public class SearchResultDTO
        {
            public string CompanyId { get; set; }
            public double Distance { get; set; }
        }

    }
}