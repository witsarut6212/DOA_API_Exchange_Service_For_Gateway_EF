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

        public ApplicationVerifyController(IApplicationVerifyService verifyService)
        {
            _verifyService = verifyService;
        }

        [HttpPost("verify")]
        public async Task<IActionResult> Verify([FromBody] ApplicationVerifyRequest request)
        {
            var title = "API Exchange Service For Gateway";

            if (!ModelState.IsValid)
            {
                return BadRequest(ResponseWriter.CreateError(title, "Invalid request body.", 400));
            }

            var result = await _verifyService.VerifyApplicationAsync(request);

            if (result.StatusCode != 200)
            {
                return StatusCode(result.StatusCode, ResponseWriter.CreateError(title, result.Message, result.StatusCode));
            }

            return Ok(ResponseWriter.CreateSuccess(title, result.Data, result.Message));
        }
    }
}
