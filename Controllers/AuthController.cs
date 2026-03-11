using Microsoft.AspNetCore.Mvc;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;
using DOA_API_Exchange_Service_For_Gateway.Services;
using DOA_API_Exchange_Service_For_Gateway.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;
using DOA_API_Exchange_Service_For_Gateway.Middlewares;

namespace DOA_API_Exchange_Service_For_Gateway.Controllers
{
    [Route("auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IResponseHelper _response;

        public AuthController(IAuthService authService, IResponseHelper response)
        {
            _authService = authService;
            _response = response;
        }

        [HttpPost("token")]
        [ServiceFilter(typeof(ClientIdAuthFilter))]
        public async Task<IActionResult> GenerateToken([FromBody] AuthTokenRequest request)
        {
            var clientId = HttpContext.Items["client_id"]?.ToString();

            var result = await _authService.IssueTokenAsync(clientId, request?.CredentialType);

            if (!result.Success)
            {
                return StatusCode(result.StatusCode, _response.CreateError(
                    result.Message,
                    result.StatusCode,
                    null,
                    result.Validations));
            }

            return Ok(_response.CreateSuccess(result.Data, result.Message));
        }
    }
}
