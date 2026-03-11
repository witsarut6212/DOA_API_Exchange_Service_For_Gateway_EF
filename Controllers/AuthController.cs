using Microsoft.AspNetCore.Mvc;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;
using DOA_API_Exchange_Service_For_Gateway.Services;
using DOA_API_Exchange_Service_For_Gateway.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DOA_API_Exchange_Service_For_Gateway.Controllers
{
    [Route("auth")]
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


        [HttpPost("token")]
        public async Task<IActionResult> GenerateToken([FromBody] AuthTokenRequest request)
        {
            var title = _configuration["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";

            // 1. Read client_id from Header
            if (!Request.Headers.TryGetValue("client_id", out var clientIdValues) &&
                !Request.Headers.TryGetValue("cliend_id", out clientIdValues))
            {
                return BadRequest(ResponseWriter.CreateError(title, "client_id header is required.", 401, HttpContext.TraceIdentifier, HttpContext.Request.Path));
            }

            var clientId = clientIdValues.ToString();

            // 2. Delegate logic to Service
            var result = await _authService.IssueTokenAsync(clientId, request?.CredentialType);

            // 3. Handle Result
            if (!result.Success)
            {
                return StatusCode(result.StatusCode, ResponseWriter.CreateError(
                    title,
                    result.Message,
                    result.StatusCode,
                    HttpContext.TraceIdentifier,
                    HttpContext.Request.Path,
                    null,
                    result.Validations));
            }

            return Ok(ResponseWriter.CreateSuccess(title, result.Data, result.Message));
        }
    }
}
