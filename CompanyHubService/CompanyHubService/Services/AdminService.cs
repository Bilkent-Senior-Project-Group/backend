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
                    Phone = string.IsNullOrEmpty(companyDto.Phone) ? "Unknown" : companyDto.Phone, // ✅ Default value
                    Email = string.IsNullOrEmpty(companyDto.Email) ? "Unknown" : companyDto.Email, // ✅ Default value
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
                        Phone = companyDto.Phone,
                        Email = companyDto.Email,
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


    }
}
