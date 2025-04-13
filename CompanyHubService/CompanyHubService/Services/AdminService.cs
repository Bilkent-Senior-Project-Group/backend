using System.Diagnostics.Eventing.Reader;
using CompanyHubService.Data;
using CompanyHubService.Models;
using CompanyHubService.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using Confluent.Kafka;

namespace CompanyHubService.Services
{
    public class AdminService
    {
        private CompanyHubDbContext _dbContext { get; set; }
        private UserManager<User> _userManager { get; set; }

        private readonly IProducer<string, string> _kafkaProducer;
        public AdminService(CompanyHubDbContext dbContext, UserManager<User> userManager, IProducer<string, string> kafkaProducer)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _kafkaProducer = kafkaProducer;
        }



        public async Task<List<Company>> CompaniesToBeVerified()
        {
            var companies = await _dbContext.Companies.ToListAsync();
            companies = companies.Where(c => c.Verified == false).ToList();

            return companies;
        }

        public async Task<Company> VerifyCompany(Guid CompanyId)
        {
            var company = await _dbContext.Companies.FirstOrDefaultAsync(c => c.CompanyId == CompanyId);
            if (company == null)
            {
                return null;
            }

            company.Verified = true;
            await _dbContext.SaveChangesAsync();

            var userCompany = await _dbContext.UserCompanies
            .Where(uc => uc.CompanyId == CompanyId)
            .OrderBy(uc => uc.AddedAt)
            .FirstOrDefaultAsync();

            if (userCompany != null)
            {
                var user = await _userManager.FindByIdAsync(userCompany.UserId);
                if (user != null)
                {
                    await _userManager.RemoveFromRoleAsync(user, "VerifiedUser");
                    await _userManager.AddToRoleAsync(user, "Root");
                }
            }

            return company;
        }

        public async Task<bool> AddCompanyAsync(CreateCompanyRequestDTO companyDto)
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

                Console.WriteLine($"Processing Company: {companyDto.CompanyName}");

                // Create new company entity
                var company = new Company
                {
                    CompanyId = Guid.NewGuid(),
                    CompanyName = companyDto.CompanyName,
                    Description = companyDto.Description,
                    Location = companyDto.Location,
                    Website = companyDto.Website,
                    CompanySize = companyDto.CompanySize,
                    FoundedYear = companyDto.FoundedYear,
                    LogoUrl = "https://default.logo.url/logo.png",
                    Phone = string.IsNullOrEmpty(companyDto.Phone) ? "Unknown" : companyDto.Phone, // ✅ Default value
                    Email = string.IsNullOrEmpty(companyDto.Email) ? "Unknown" : companyDto.Email, // ✅ Default value
                    Verified = true,
                    Address = string.IsNullOrEmpty(companyDto.Address) ? "Unknown" : companyDto.Address, // ✅ Default value 
                };

                // Create ServiceCompany mappings
                var serviceCompanies = companyDto.Services.Select(item => new ServiceCompany
                {
                    CompanyId = company.CompanyId,
                    ServiceId = item,
                }).ToList();

                _dbContext.Companies.Add(company);
                await _dbContext.SaveChangesAsync();

                Console.WriteLine($"Successfully added company: {company.CompanyName}");

                

                // Add the project-service mappings to the DbContext in a single operation
                if (serviceCompanies.Any())
                {
                    _dbContext.ServiceCompanies.AddRange(serviceCompanies);
                }

                // Add projects (portfolio) to Projects table


                /*if (companyDto.Portfolio != null && companyDto.Portfolio.Any())
                {
                    Console.WriteLine("📌 Adding Projects...");
                    foreach (var projectDto in companyDto.Portfolio)
                    {
                        var project = new Project
                        {
                            ProjectId = Guid.NewGuid(),
                            ProjectName = projectDto.ProjectName,
                            Description = projectDto.Description,
                            TechnologiesUsed = string.Join(", ", projectDto.TechnologiesUsed),
                            ClientType = projectDto.ClientType,
                            StartDate = projectDto.StartDate,
                            CompletionDate = projectDto.CompletionDate,
                            IsOnCompedia = false,
                            IsCompleted = projectDto.IsCompleted,
                            ProjectUrl = projectDto.ProjectUrl,
                            //services eklenecek
                        };

                        _dbContext.Projects.Add(project);
                        Console.WriteLine($"Added project: {project.ProjectName}");
                    }
                }*/

                List<Project> projects = new List<Project>();

                if (companyDto.Portfolio != null && companyDto.Portfolio.Any())
                {
                    var projectCompanies = new List<ProjectCompany>();
                    var projectServices = new List<ServiceProject>(); // List to hold project-service mappings

                    foreach (var p in companyDto.Portfolio)
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

        /*public async Task<bool> BulkAddCompaniesAsync(BulkCompanyInsertDTO bulkCompanies)
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
                        CompanyName = companyDto.CompanyName,
                        Location = companyDto.Location,
                        Website = companyDto.Website,
                        CompanySize = companyDto.CompanySize,
                        FoundedYear = companyDto.FoundedYear,
                        Phone = companyDto.Phone,
                        Email = companyDto.Email,
                        Address = companyDto.Address,
                        Verified = true,
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
        }*/

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

                Console.WriteLine($"\ud83d\udccc Processing {bulkCompanies.Companies.Count} Companies...");

                var companiesToInsert = new List<Company>();
                var projectsToInsert = new List<Project>();
                var projectCompaniesToInsert = new List<ProjectCompany>();
                var serviceCompaniesToInsert = new List<ServiceCompany>();
                var projectServicesToInsert = new List<ServiceProject>();

                foreach (var companyDto in bulkCompanies.Companies)
                {
                    var companyId = Guid.NewGuid();

                    var company = new Company
                    {
                        CompanyId = companyId,
                        CompanyName = companyDto.CompanyName,
                        Description = companyDto.Description,
                        Location = companyDto.Location,
                        Website = companyDto.Website,
                        CompanySize = companyDto.CompanySize,
                        FoundedYear = companyDto.FoundedYear,
                        Phone = companyDto.Phone ?? "Unknown",
                        Email = companyDto.Email ?? "Unknown",
                        Address = companyDto.Address ?? "Unknown",
                        Verified = true,
                        LogoUrl = "https://default.logo.url/logo.png"
                    };

                    companiesToInsert.Add(company);

                    // Add the company and service mappings to the DbContext
                    _dbContext.Companies.Add(company);
                    await _dbContext.SaveChangesAsync();

                    if (companyDto.Services != null && companyDto.Services.Any())
                    {
                        serviceCompaniesToInsert.AddRange(companyDto.Services.Select(serviceId => new ServiceCompany
                        {
                            CompanyId = companyId,
                            ServiceId = serviceId
                        }));
                    }
                    
                    if (companyDto.Portfolio != null && companyDto.Portfolio.Any())
                    {
                        foreach (var projectDto in companyDto.Portfolio)
                        {
                            var projectId = Guid.NewGuid();

                            var project = new Project
                            {
                                ProjectId = projectId,
                                ProjectName = projectDto.ProjectName,
                                Description = projectDto.Description,
                                TechnologiesUsed = string.Join(", ", projectDto.TechnologiesUsed ?? new List<string>()),
                                ClientType = projectDto.ClientType,
                                StartDate = projectDto.StartDate,
                                CompletionDate = projectDto.CompletionDate,
                                ProjectUrl = projectDto.ProjectUrl,
                                IsCompleted = projectDto.IsCompleted,
                                IsOnCompedia = false
                            };

                            projectsToInsert.Add(project);

                            projectCompaniesToInsert.Add(new ProjectCompany
                            {
                                ProjectId = projectId,
                                ClientCompanyId = companyId,
                                ProviderCompanyId = companyId, // IT CAN NOT BE NULL, HERE IS THE ERROR
                                IsClient = 1,
                                OtherCompanyName = projectDto.ProviderCompanyName
                            });

                            if (projectDto.Services != null && projectDto.Services.Any())
                            {
                                projectServicesToInsert.AddRange(projectDto.Services.Select(service => new ServiceProject
                                {
                                    ProjectId = projectId,
                                    ServiceId = service
                                }));
                            }
                        }
                    }
                }

                await _dbContext.ServiceCompanies.AddRangeAsync(serviceCompaniesToInsert);
                await _dbContext.Projects.AddRangeAsync(projectsToInsert);
                await _dbContext.ProjectCompanies.AddRangeAsync(projectCompaniesToInsert);
                await _dbContext.ServiceProjects.AddRangeAsync(projectServicesToInsert);

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                Console.WriteLine("All companies and projects added successfully.");

                var mappedCompaniesForKafka = companiesToInsert.Select(company => new
                {
                    id = company.CompanyId,
                    name = company.CompanyName,
                    location = company.Location,
                    //services eklenecek
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
                    var messageValue = System.Text.Json.JsonSerializer.Serialize(mappedCompaniesForKafka);

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




    }
}
