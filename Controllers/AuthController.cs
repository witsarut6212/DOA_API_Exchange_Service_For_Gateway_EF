using Microsoft.AspNetCore.Mvc;
using DOA_API_Exchange_Service_For_Gateway.Models;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;
using DOA_API_Exchange_Service_For_Gateway.Services;
using DOA_API_Exchange_Service_For_Gateway.Helpers;

namespace DOA_API_Exchange_Service_For_Gateway.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;

        public AuthController(IAuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
        }

        [HttpPost("login-mockup")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var title = _configuration["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";

            if (_authService.ValidateCredentials(request.Username, request.Password))
            {
                var token = _authService.GenerateJwtToken(request.Username);
                var data = new Dictionary<string, string> { { "token", token } };
                return Ok(ResponseWriter.CreateSuccess(title, data, "Authentication was susccessful."));
            }

            var errorResponse = ResponseWriter.CreateError(title, "Authentication failed.", 401, HttpContext.TraceIdentifier, HttpContext.Request.Path);
            return Unauthorized(errorResponse);
        }
    }
}
