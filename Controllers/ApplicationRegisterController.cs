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
        private readonly IResponseHelper _response;

        public ApplicationRegisterController(
            IApplicationRegistrationService registrationService,
            IResponseHelper response)
        {
            _registrationService = registrationService;
            _response = response;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] ApplicationRegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(_response.CreateError("Invalid request body.", 400));
            }

            var result = await _registrationService.RegisterApplicationAsync(request);

            if (!result.Success)
            {
                if (result.Message.Contains("already registered"))
                {
                    return Conflict(_response.CreateError(result.Message, 409));
                }
                return StatusCode(500, _response.CreateError(result.Message, 500));
            }

            return Ok(_response.CreateSuccess(result.Data, result.Message));
        }
    }
}
