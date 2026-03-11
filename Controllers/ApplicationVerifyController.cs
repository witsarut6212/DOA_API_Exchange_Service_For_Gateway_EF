using Microsoft.AspNetCore.Mvc;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;
using DOA_API_Exchange_Service_For_Gateway.Helpers;
using DOA_API_Exchange_Service_For_Gateway.Services;

namespace DOA_API_Exchange_Service_For_Gateway.Controllers
{
    [ApiController]
    [Route("application")]
    public class ApplicationVerifyController : ControllerBase
    {
        private readonly IApplicationVerifyService _verifyService;
        private readonly IResponseHelper _response;

        public ApplicationVerifyController(
            IApplicationVerifyService verifyService,
            IResponseHelper response)
        {
            _verifyService = verifyService;
            _response = response;
        }

        [HttpPost("verify")]
        public async Task<IActionResult> Verify([FromBody] ApplicationVerifyRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(_response.CreateError("Invalid request body.", 400));
            }

            var result = await _verifyService.VerifyApplicationAsync(request);

            if (result.StatusCode != 200)
            {
                return StatusCode(result.StatusCode, _response.CreateError(result.Message, result.StatusCode));
            }

            return Ok(_response.CreateSuccess(result.Data, result.Message));
        }
    }
}
