using CompanyHubService.Data;
using CompanyHubService.DTOs;
using CompanyHubService.Models;
using CompanyHubService.Services;
using CompanyHubService.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
namespace CompanyHubService.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]

    public class AdminController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly AdminService _adminService;
        private readonly UserManager<User> _userManager;
        private readonly CompanyHubDbContext _dbContext;
        private readonly CompanyService _companyService;

        public AdminController(AuthService authService, AdminService adminService, UserManager<User> userManager, CompanyHubDbContext dbContext, CompanyService companyService)
        {
            _authService = authService;
            _adminService = adminService;
            _userManager = userManager;
            _dbContext = dbContext;
            _companyService = companyService;
        }

        [HttpGet("CompaniesToBeVerified")]
        public async Task<ActionResult<List<Company>>> CompaniesToBeVerified()
        {
            // Log or breakpoint here
            Console.WriteLine("CompaniesToBeVerified endpoint hit.");

            var companies = await _adminService.CompaniesToBeVerified();

            if (companies == null)
            {
                return BadRequest("No companies found.");
            }
            return Ok(companies);
        }

        [HttpPut("VerifyCompany")]
        public async Task<IActionResult> VerifyCompany([FromBody] Guid CompanyId)
        {
            var company = await _adminService.VerifyCompany(CompanyId);
            if (company == null)
            {
                return BadRequest(new { Message = "Company not found." });
            }

            return Ok(new { Message = "Company verified." });
        }

        //This is the one where company is added from a given json file with muitple companies (only admin should be able to)
        [HttpPost("BulkAddCompanies")]
        public async Task<IActionResult> BulkAddCompanies([FromBody] Dictionary<string, CompanyProfileDTO> jsonCompanies)
        {
            if (jsonCompanies == null || jsonCompanies.Count == 0)
            {
                return BadRequest(new { message = "No companies found in the request." });
            }

            // Convert dictionary to a list of CompanyProfileDTO
            var bulkCompanies = new BulkCompanyInsertDTO
            {
                Companies = jsonCompanies.Select(entry =>
                {
                    var companyDto = entry.Value;
                    companyDto.Website = entry.Key;  // The JSON key is the website
                    return companyDto;
                }).ToList()
            };

            var result = await _adminService.BulkAddCompaniesAsync(bulkCompanies);

            if (result)
                return Ok(new { message = "All companies added successfully." });

            return BadRequest(new { message = "Failed to add companies." });
        }

        //This is the one where company is added from a given json file with one company (only admin should be able to)
        [HttpPost("AddCompany")]
        public async Task<IActionResult> AddCompany([FromBody] CompanyProfileDTO companyDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { Message = "Invalid input parameters." });
            }

            var result = await _adminService.AddCompanyAsync(companyDto);

            if (!result)
            {
                return BadRequest(new { Message = "Failed to add company." });
            }

            return Ok(new { Message = "Company successfully added." });
        }

        [HttpPost("ApproveUser/{userId}")]
        public async Task<IActionResult> ApproveUser([FromBody] string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            return Ok(new { Message = "User email confirmed successfully." });
        }
    }
}
