using Microsoft.AspNetCore.Mvc;
using DOA_API_Exchange_Service_For_Gateway.Helpers;

namespace DOA_API_Exchange_Service_For_Gateway.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public HealthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult CheckHealth()
        {
            var title = _configuration["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";
            
            var data = new { status = "healthy" };
            
            return Ok(ResponseWriter.CreateSuccess(title, data, "API working properly"));
        }
    }
}
