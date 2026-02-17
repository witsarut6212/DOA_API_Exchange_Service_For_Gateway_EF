using Microsoft.AspNetCore.Mvc;
using DOA_API_Exchange_Service_For_Gateway.Models;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;
using DOA_API_Exchange_Service_For_Gateway.Services;

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
                //return Ok(new { token });
                //return Unauthorized(new ApiResponse<object>
                //{
                //    Info = new ApiInfo
                //    {
                //        Title = title,
                //        Detail = "Authentication was susccessful.",
                //        SystemCode = 200
                //    },
                //});
                return Ok(new ApiResponse<object>
                {
                    Info = new ApiInfo
                    {
                        Title = title,
                        Detail = "Authentication was susccessful.",
                        SystemCode = 200
                    },
                    Data = new Dictionary<string, string>
                {
                    { "token", token }
                }
                });
            }

            return Unauthorized(new ApiResponse<object>
            {
                Info = new ApiInfo
                {
                    Title = title,
                    Detail = "Incorrect credentials: Entering the wrong username or password.",
                    SystemCode = 401
                }
            });
        }
    }
}
