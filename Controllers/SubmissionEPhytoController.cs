using DOA_API_Exchange_Service_For_Gateway.Models;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;
using DOA_API_Exchange_Service_For_Gateway.Services;
using DOA_API_Exchange_Service_For_Gateway.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Security.Claims;

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
        public async Task<IActionResult> UpdateProgress([FromBody] JObject rawRequest)
        {
            var title = _config["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";
            var request = rawRequest.ToObject<EPhytoProgressRequest>();

            if (request == null || request.DocumentControl == null)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Info = new ApiInfo { Title = title, Status = 400, Detail = "Invalid request format." },
                    Error = new ApiError { TraceId = HttpContext.TraceIdentifier, Instance = HttpContext.Request.Path }
                });
            }

            // Step 0: Intercept Duplicate DocumentNumber (As requested by auditor)
            if (await _submissionService.IsDocumentNumberDuplicateAsync(request.DocumentControl.DocumentNumber))
            {
                return Conflict(new ApiResponse<object>
                {
                    Info = new ApiInfo 
                    { 
                        Title = title, 
                        Status = 409, 
                        Detail = $"Duplicate DocumentNumber found: '{request.DocumentControl.DocumentNumber}'. This document already exists in the system." 
                    },
                    Error = new ApiError 
                    { 
                        TraceId = HttpContext.TraceIdentifier, 
                        Instance = HttpContext.Request.Path 
                    }
                });
            }

            // Extract AppNickName from JWT
            var appNickName = User.FindFirstValue("AppNickName") ?? "SYSTEM_PROGRESS";
 
            // Step 1: Save Payload
            var payloadId = await _submissionService.SaveResponsePayloadAsync(rawRequest.ToString(Formatting.None), appNickName, request.DocumentControl.ReferenceNumber);

            if (payloadId == 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Info = new ApiInfo { Title = title, Status = 400, Detail = "Failed to create response payload." },
                    Error = new ApiError { TraceId = HttpContext.TraceIdentifier, Instance = HttpContext.Request.Path }
                });
            }

            // Step 2: Queue for background processing
            _queue.Enqueue(payloadId, request, appNickName);

            return Ok(new ApiResponse<object>
            {
                Info = new ApiInfo { Title = title, Status = 200, Detail = "Successfully received ePhyto progress." },
                Data = new 
                { 
                    ReferenceNumber = request.DocumentControl.ReferenceNumber, 
                    Status = request.DocumentControl.ResponseInfo.Status 
                }
            });
        }
    }
}
