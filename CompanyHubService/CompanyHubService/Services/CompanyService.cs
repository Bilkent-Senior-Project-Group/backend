using CompanyHubService.Data;
using CompanyHubService.DTOs;
using CompanyHubService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Confluent.Kafka;
using System.Text.Json;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;

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

        //used when a new user creates a company.
        public async Task<bool> CreateCompanyAsync(CreateCompanyRequestDTO request, string userId)
        {

            var company = new Company
            {
                CompanyId = Guid.NewGuid(),
                CompanyName = request.CompanyName,
                Description = request.Description,
                FoundedYear = request.FoundedYear,
                Address = request.Address,
                Specialties = request.Specialties,
                Industries = request.Industries,
                Location = request.Location,
                Website = request.Website,
                CompanySize = request.CompanySize,
                Verified = false,
                ContactInfo = request.ContactInfo,
                CoreExpertise = request.CoreExpertise
            };


            _dbContext.Companies.Add(company);
            await _dbContext.SaveChangesAsync();

            var userCompany = new UserCompany
            {
                UserId = userId,
                CompanyId = company.CompanyId,
            };

            _dbContext.UserCompanies.Add(userCompany);
            await _dbContext.SaveChangesAsync();

            // Check whether there is any portfolio is stored, if there are list them inside projects and store in Projects.
            if (request.Portfolio != null && request.Portfolio.Count > 0)
            {
                var projects = request.Portfolio.Select(p => new Project
                {
                    ProjectId = Guid.NewGuid(),
                    ProjectName = p.ProjectName,
                    Description = p.Description,
                    TechnologiesUsed = string.Join(", ", p.TechnologiesUsed),
                    Industry = p.Industry,
                    ClientType = p.ClientType,
                    Impact = p.Impact,
                    StartDate = p.StartDate,
                    CompletionDate = p.CompletionDate,
                    IsOnCompedia = false,
                    IsCompleted = p.IsCompleted,
                    ProjectUrl = p.ProjectUrl
                }).ToList();

                _dbContext.Projects.AddRange(projects);
                await _dbContext.SaveChangesAsync();

                var projectCompanies = projects.Select(p => new ProjectCompany
                {
                    ProjectId = p.ProjectId,
                    ClientCompanyId = company.CompanyId,
                    ProviderCompanyId = null
                }).ToList();

                _dbContext.ProjectCompanies.AddRange(projectCompanies);
                await _dbContext.SaveChangesAsync();

            }

            return true;
        }


        // Used when importing company data from external source
        public async Task<bool> AddCompanyAsync(CompanyProfileDTO companyDto)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {

                Console.WriteLine("Starting AddCompanyAsync...");

                if (companyDto == null)
                {
                    Console.WriteLine("companyDto is NULL!");
                    return false;
                }

                Console.WriteLine($"Processing Company: {companyDto.Name}");

                // Create new company entity
                var company = new Company
                {
                    CompanyId = Guid.NewGuid(),
                    CompanyName = companyDto.Name,
                    Specialties = companyDto.Specialties,
                    Industries = string.Join(", ", companyDto.Industries ?? new List<string>()), // Convert list to string
                    CoreExpertise = string.Join(", ", companyDto.CoreExpertise ?? new List<string>()),
                    Location = companyDto.Location,
                    Website = companyDto.Website,
                    CompanySize = companyDto.CompanySize,
                    FoundedYear = companyDto.FoundedYear,
                    ContactInfo = companyDto.ContactInfo,
                    Verified = companyDto.Verified == 1, // Convert int to bool
                    Address = string.IsNullOrEmpty(companyDto.Address) ? "Unknown" : companyDto.Address, // ✅ Default value
                };

                _dbContext.Companies.Add(company);
                await _dbContext.SaveChangesAsync();

                Console.WriteLine($"Successfully added company: {company.CompanyName}");

                // Add projects (portfolio) to Projects table


                if (companyDto.Projects != null && companyDto.Projects.Any())
                {
                    Console.WriteLine("📌 Adding Projects...");
                    foreach (var projectDto in companyDto.Projects)
                    {
                        var project = new Project
                        {
                            ProjectId = Guid.NewGuid(),
                            ProjectName = projectDto.ProjectName,
                            Description = projectDto.Description,
                            TechnologiesUsed = string.Join(", ", projectDto.TechnologiesUsed),
                            Industry = projectDto.Industry,
                            ClientType = projectDto.ClientType,
                            Impact = projectDto.Impact,
                            StartDate = projectDto.StartDate,
                            CompletionDate = projectDto.CompletionDate,
                            IsOnCompedia = false,
                            IsCompleted = projectDto.IsCompleted,
                            ProjectUrl = projectDto.ProjectUrl
                        };

                        _dbContext.Projects.Add(project);
                        Console.WriteLine($"Added project: {project.ProjectName}");
                    }
                }

                else
                {
                    Console.WriteLine("No projects found in portfolio.");
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                Console.WriteLine("Transaction committed. Company added successfully.");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ModifyCompanyProfileAsync(CompanyProfileDTO companyProfileDTO)
        {
            try
            {
                // Serialize the DTO to JSON
                var messageValue = System.Text.Json.JsonSerializer.Serialize(companyProfileDTO);

                // Create the Kafka message
                var message = new Message<string, string>
                {
                    Key = companyProfileDTO.CompanyId.ToString(), // Use CompanyId as the key
                    Value = messageValue                          // JSON payload as the value
                };

                var result = await _kafkaProducer.ProduceAsync("modifyCompanySpecialty", message);
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
                        Specialties = companyDto.Specialties,
                        Industries = string.Join(", ", companyDto.Industries ?? new List<string>()),
                        CoreExpertise = string.Join(", ", companyDto.CoreExpertise ?? new List<string>()),
                        Location = companyDto.Location,
                        Website = companyDto.Website,
                        CompanySize = companyDto.CompanySize,
                        FoundedYear = companyDto.FoundedYear,
                        ContactInfo = companyDto.ContactInfo,
                        Address = companyDto.Address,
                        Verified = companyDto.Verified == 0
                    };

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
                                Industry = projectDto.Industry,
                                ClientType = projectDto.ClientType,
                                Impact = projectDto.Impact,
                                StartDate = projectDto.StartDate,
                                ProjectUrl = projectDto.ProjectUrl
                            };

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
                    specialties = company.Specialties,
                    core_expertise = company.CoreExpertise,
                    industries = company.Industries,
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

        public async Task<object> FreeTextSearchAsync(string searchQuery)
        {
            // Define FastAPI URL
            string fastApiUrl = "http://127.0.0.1:8000/search";  // Adjust if needed

            // Prepare request body
            var payload = new { query = searchQuery };
            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                // Send POST request to FastAPI
                var client = _httpClientFactory.CreateClient();
                HttpResponseMessage response = await client.PostAsync(fastApiUrl, content);

                // Ensure success response
                response.EnsureSuccessStatusCode();

                // Parse response
                string jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<object>(jsonResponse);
            }
            catch (Exception ex)
            {
                // Handle errors
                return new { error = "Failed to connect to FastAPI", details = ex.Message };
            }
        }
    }
}