using DOA_API_Exchange_Service_For_Gateway.Models;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;
using DOA_API_Exchange_Service_For_Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DOA_API_Exchange_Service_For_Gateway.Controllers
{
    [ApiController]
    [Route("submission/ephyto")]
    [Authorize] // ตู้อัตโนมัติควรมีการยืนยันตัวตน
    public class SubmissionEPhytoController : ControllerBase
    {
        private readonly ISubmissionService _submissionService;
        private readonly IConfiguration _config;

        public SubmissionEPhytoController(ISubmissionService submissionService, IConfiguration config)
        {
            _submissionService = submissionService;
            _config = config;
        }

        [HttpPost("progress")]
        public async Task<IActionResult> UpdateProgress([FromBody] EPhytoProgressRequest request)
        {
            var title = _config["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";

            var result = await _submissionService.UpdateProgressAsync(request);

            if (result)
            {
                return Ok(new ApiResponse<object>
                {
                    Info = new ApiInfo
                    {
                        Title = title,
                        Status = 200,
                        Detail = "Successfully updated ePhyto progress."
                    },
                    Data = new { MessageId = request.MessageId, Status = request.Status }
                });
            }

            return BadRequest(new ApiResponse<object>
            {
                Info = new ApiInfo
                {
                    Title = title,
                    Status = 400,
                    Detail = "Failed to update ePhyto progress."
                },
                Error = new ApiError
                {
                    TraceId = HttpContext.TraceIdentifier,
                    Instance = HttpContext.Request.Path
                }
            });
        }
    }
}
