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

namespace CompanyHubService.Services
{
    public class CompanyService
    {
        private CompanyHubDbContext _dbContext { get; set; }
        private UserService userService { get; set; }
        private readonly IProducer<string, string> _kafkaProducer;
        private readonly IHttpClientFactory _httpClientFactory;

        public CompanyService(CompanyHubDbContext dbContext, UserService userService, IProducer<string, string> kafkaProducer, IHttpClientFactory httpClientFactory)
        {
            _dbContext = dbContext;
            this.userService = userService;
            _kafkaProducer = kafkaProducer;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<bool> CreateCompanyAsync(CreateCompanyRequestDTO request, string userId)
        {
            var company = new Company
            {
                CompanyId = Guid.NewGuid(),
                CompanyName = request.CompanyName,
                Description = request.Description,
                FoundedYear = request.FoundedYear,
                Address = request.Address,
                Website = request.Website,
                CompanySize = request.CompanySize,
                Verified = false,
                Phone = request.Phone,
                Email = request.Email,
                LogoUrl = "https://default.logo.url/logo.png"

            };

            // Create ServiceCompany mappings
            var serviceCompanies = request.Services.Select(item => new ServiceCompany
            {
                CompanyId = company.CompanyId,
                ServiceId = item,
            }).ToList();

            // Add the company and service mappings to the DbContext
            _dbContext.Companies.Add(company);

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
                        ClientCompanyId = clientCompany?.CompanyId, // Null if not found
                        ProviderCompanyId = providerCompany?.CompanyId // Null if not found
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

            // Send Kafka message for company creation
            try
            {
                var companyKafkaDTO = new CompanyKafkaDTO
                {
                    CompanyId = company.CompanyId,
                    Description = company.Description,
                    FoundedYear = company.FoundedYear,
                    CompanySize = company.CompanySize,
                    Location = company.Location,
                    OverallRating = company.OverallRating,
                    ServiceCompanies = serviceCompanies,
                    Projects = projects,
                    Products = new List<Product>(), // Empty as new company
                    Reviews = new List<Review>() // Empty as new company
                };

                // Serialize the DTO to JSON
                var messageValue = System.Text.Json.JsonSerializer.Serialize(companyKafkaDTO);

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


        public async Task<bool> ModifyCompanyProfileAsync(CompanyProfileDTO companyProfileDTO)
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
                var messageValue = System.Text.Json.JsonSerializer.Serialize(companyKafkaDTO);

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
                    Email = uc.User.Email,
                    PhoneNumber = uc.User.PhoneNumber,
                })
                .ToListAsync();

            return users;
        }

        public async Task<List<CompanyDTO>> GetCompaniesOfUserAsync(string userId)
        {
            var companies = await _dbContext.UserCompanies
                .Where(uc => uc.UserId == userId)
                .Include(uc => uc.Company)
                .Select(uc => new CompanyDTO
                {
                    CompanyId = uc.Company.CompanyId,
                    CompanyName = uc.Company.CompanyName
                })
                .ToListAsync();

            return companies;
        }

        public async Task<bool> BulkAddCompaniesAsync(BulkCompanyInsertDTO bulkCompanies)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                if (bulkCompanies.Companies == null || !bulkCompanies.Companies.Any())
                {
                    Console.WriteLine("No companies found in the input JSON.");
                    return false;
                }

                Console.WriteLine($"📌 Processing {bulkCompanies.Companies.Count} Companies...");

                var companiesToInsert = new List<Company>();
                var projectsToInsert = new List<Project>();
                var projectCompaniesToInsert = new List<ProjectCompany>();

                foreach (var companyDto in bulkCompanies.Companies)
                {
                    var companyId = Guid.NewGuid();

                    var company = new Company
                    {
                        CompanyId = companyId,
                        CompanyName = companyDto.Name,
                        Location = companyDto.Location,
                        Website = companyDto.Website,
                        CompanySize = companyDto.CompanySize,
                        FoundedYear = companyDto.FoundedYear,
                        Phone = companyDto.Phone,
                        Email = companyDto.Email,
                        Address = companyDto.Address,
                        Verified = companyDto.Verified == 0,
                    };

                    _dbContext.ServiceCompanies.AddRange(companyDto.Services);

                    companiesToInsert.Add(company);

                    if (companyDto.Projects != null && companyDto.Projects.Any())
                    {
                        foreach (var projectDto in companyDto.Projects)
                        {
                            var project = new Project
                            {
                                ProjectId = Guid.NewGuid(),
                                ProjectName = projectDto.ProjectName,
                                Description = projectDto.Description,
                                TechnologiesUsed = string.Join(", ", projectDto.TechnologiesUsed ?? new List<string>()),
                                ClientType = projectDto.ClientType,
                                StartDate = projectDto.StartDate,
                                ProjectUrl = projectDto.ProjectUrl
                            };

                            if (projectDto.Services != null && projectDto.Services.Any())
                            {
                                var projectServiceMappings = projectDto.Services.Select(serviceId => new ServiceProject
                                {
                                    ProjectId = project.ProjectId,
                                    ServiceId = serviceId.Id
                                }).ToList();

                                projectServiceMappings.AddRange(projectServiceMappings);

                                _dbContext.ServiceProjects.AddRange(projectServiceMappings);
                            }

                            projectsToInsert.Add(project);
                        }
                    }
                }

                foreach (var project in projectsToInsert)
                {
                    var clientCompanyId = companiesToInsert
                        .Where(c => c.CompanyId == project.ProjectCompany.ClientCompanyId)
                        .Select(c => c.CompanyId)
                        .FirstOrDefault();

                    if (clientCompanyId != Guid.Empty) // Ensure a valid ID is found
                    {
                        projectCompaniesToInsert.Add(new ProjectCompany
                        {
                            ProjectId = project.ProjectId,
                            ClientCompanyId = clientCompanyId,
                            ProviderCompanyId = null // Or assign a valid provider if available
                        });
                    }
                }


                // Bulk Insert
                await _dbContext.Companies.AddRangeAsync(companiesToInsert);
                await _dbContext.Projects.AddRangeAsync(projectsToInsert);
                await _dbContext.ProjectCompanies.AddRangeAsync(projectCompaniesToInsert);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                Console.WriteLine("All companies and projects added successfully.");

                var mappedCompaniesForKafka = companiesToInsert.Select(company => new
                {
                    id = company.CompanyId,
                    name = company.CompanyName,
                    //services eklenecek
                    location = company.Location,
                    technologies_used = projectsToInsert
                    .Where(p => projectCompaniesToInsert
                        .Any(pc => pc.ProjectId == p.ProjectId && pc.ClientCompanyId == company.CompanyId))
                    .Select(p => p.TechnologiesUsed)
                    .Distinct()
                    .ToList(),
                    company_size = company.CompanySize,
                    founded_year = company.FoundedYear
                }).ToList();

                try
                {
                    // Serialize the DTO to JSON
                    var messageValue = System.Text.Json.JsonSerializer.Serialize(mappedCompaniesForKafka);

                    // Create the Kafka message
                    var message = new Message<string, string>
                    {
                        Key = DateTime.Now.ToString(),
                        Value = messageValue
                    };

                    var result = await _kafkaProducer.ProduceAsync("addBulkCompanies", message);
                    Console.WriteLine($"Sent message to Kafka topic {result.TopicPartitionOffset}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error producing Kafka message: {ex.Message}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error during bulk insert: {ex.Message}");
                return false;
            }
        }
        public async Task<string> FreeTextSearchAsync(string searchQuery)
        {
            string fastApiUrl = "http://127.0.0.1:8000/search";

            var payload = new { query = searchQuery };
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
                    .Select(c => new
                    {
                        CompanyId = c.CompanyId,
                        Name = c.CompanyName,
                        Size = c.CompanySize.ToString(),
                        Location = c.Location,
                        Description = c.Description
                    })
                    .ToListAsync();

                // Merge SQL data with distances from FastAPI
                var enrichedResults = searchResults.Results
                    .Join(companies,
                          r => Guid.TryParse(r.CompanyId, out var guid) ? guid : Guid.Empty,  // Convert string ID to GUID
                          c => c.CompanyId,  // This is already GUID
                          (r, c) => new
                          {
                              c.CompanyId,
                              c.Name,
                              c.Size,
                              c.Location,
                              c.Description,
                              r.Distance
                          })
                    .OrderByDescending(r => r.Distance)
                    .ToList();

                return JsonConvert.SerializeObject(new
                {
                    query = searchResults.Query,
                    extracted = searchResults.Extracted,
                    results = enrichedResults
                });
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { error = "Failed to process search", details = ex.Message });
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
            public List<string> Specialties { get; set; }
            public List<string> Industries { get; set; }
            public List<string> TechnologiesUsed { get; set; }
        }

        public class SearchResultDTO
        {
            public string CompanyId { get; set; }
            public double Distance { get; set; }
        }

    }
}