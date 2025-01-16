using CompanyHubService.Data;
using CompanyHubService.DTOs;
using CompanyHubService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Confluent.Kafka;
using System.Text.Json;

namespace CompanyHubService.Services
{
    public class CompanyService
    {
        private CompanyHubDbContext _dbContext { get; set; }
        private UserService userService { get; set; }
        private readonly IProducer<string, string> _kafkaProducer;

        public CompanyService(CompanyHubDbContext dbContext, UserService userService, IProducer<string, string> kafkaProducer)
        {
            _dbContext = dbContext;
            this.userService = userService;
            _kafkaProducer = kafkaProducer;
        }

        public async Task<bool> CreateCompanyAsync(string companyName, int foundationYear, string address, string userId, string roleId)
        {
            if (string.IsNullOrEmpty(companyName) || string.IsNullOrEmpty(address))
            {
                return false;
            }

            var company = new Company
            {
                CompanyId = Guid.NewGuid(),
                CompanyName = companyName,
                FoundationYear = foundationYear,
                CompanySize = 1,
                Address = address,
                SignupDate = DateTime.UtcNow
            };


            _dbContext.Companies.Add(company);

            var userCompany = new UserCompany
            {
                UserId = userId,
                CompanyId = company.CompanyId,
                RoleId = roleId
            };

            _dbContext.UserCompanies.Add(userCompany);
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ModifyCompanyProfileAsync(CompanyProfileDTO companyProfileDTO)
        {
            try
            {
                // Serialize the DTO to JSON
                var messageValue = JsonSerializer.Serialize(companyProfileDTO);

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
    }
}