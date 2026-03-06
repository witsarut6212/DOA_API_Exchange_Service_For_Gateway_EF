using Microsoft.AspNetCore.Mvc;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;
using DOA_API_Exchange_Service_For_Gateway.Helpers;
using DOA_API_Exchange_Service_For_Gateway.Services;

namespace DOA_API_Exchange_Service_For_Gateway.Controllers
{
    [ApiController]
    [Route("application")]
    public class ApplicationRegisterController : ControllerBase
    {
        private readonly IApplicationRegistrationService _registrationService;

        public ApplicationRegisterController(IApplicationRegistrationService registrationService)
        {
            _registrationService = registrationService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] ApplicationRegisterRequest request)
        {
            var title = "API Exchange Service For Gateway";

            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseWriter.CreateError(title, "Invalid request body.", 400));
            }

            var result = await _registrationService.RegisterApplicationAsync(request);

            if (!result.Success)
            {
                if (result.Message.Contains("already registered"))
                {
                    return Conflict(ResponseWriter.CreateError(title, result.Message, 409));
                }
                return StatusCode(500, ResponseWriter.CreateError(title, result.Message, 500));
            }

            return Ok(ResponseWriter.CreateSuccess(title, result.Data, result.Message));
        }
    }
}
