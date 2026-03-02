using DOA_API_Exchange_Service_For_Gateway.Models;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;
using DOA_API_Exchange_Service_For_Gateway.Services;
using DOA_API_Exchange_Service_For_Gateway.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DOA_API_Exchange_Service_For_Gateway.Controllers
{
    [ApiController]
    [Route("submission/ephyto")]
    [Authorize]
    public class SubmissionEPhytoController : ControllerBase
    {
        private readonly ISubmissionService _submissionService;
        private readonly IProgressQueue _queue;
        private readonly IConfiguration _config;

        public SubmissionEPhytoController(
            ISubmissionService submissionService,
            IProgressQueue queue,
            IConfiguration config)
        {
            _submissionService = submissionService;
            _queue = queue;
            _config = config;
        }

        [HttpPost("progress")]
        [ValidateProgressSchema]
        public async Task<IActionResult> UpdateProgress([FromBody] EPhytoProgressRequest request)
        {
            var title = _config["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";

            // STEP 1 ตามภาพ: Create Record response_payload (Status = WAIT)
            var result = await _submissionService.SaveResponsePayloadAsync(request);

            if (!result)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Info = new ApiInfo { Title = title, Status = 400, Detail = "Failed to create response payload." },
                    Error = new ApiError { TraceId = HttpContext.TraceIdentifier, Instance = HttpContext.Request.Path }
                });
            }

            // โยน request ไปให้ Worker (BackgroundService) จับไป "process payload"
            _queue.Enqueue(request);

            // วนมาตรง "ตอบกลับ 200" ตาม Flowchart 
            return Ok(new ApiResponse<object>
            {
                Info = new ApiInfo { Title = title, Status = 200, Detail = "Successfully received ePhyto progress." },
                // ส่งค่ากลับไปหา client โดยดึงจากโครงสร้างใหม่
                Data = new 
                { 
                    ReferenceNumber = request.DocumentControl.ReferenceNumber, 
                    Status = request.DocumentControl.ResponseInfo.Status 
                }
            });
        }
    }
}
