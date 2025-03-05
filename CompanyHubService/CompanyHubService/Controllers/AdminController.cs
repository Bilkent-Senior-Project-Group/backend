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


    }
}
