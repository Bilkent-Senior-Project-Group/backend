using Microsoft.AspNetCore.Mvc;

namespace CompanyHubService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok("Service is healthy");
        }
    }
}
