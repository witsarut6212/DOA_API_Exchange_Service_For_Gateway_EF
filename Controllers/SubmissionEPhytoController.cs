using DOA_API_Exchange_Service_For_Gateway.Models;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;
using DOA_API_Exchange_Service_For_Gateway.Services;
using DOA_API_Exchange_Service_For_Gateway.Filters;
using DOA_API_Exchange_Service_For_Gateway.Helpers;
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
        private readonly IResponseHelper _response;

        public SubmissionEPhytoController(
            ISubmissionService submissionService,
            IProgressQueue queue,
            IResponseHelper response)
        {
            _submissionService = submissionService;
            _queue = queue;
            _response = response;
        }

        [HttpPost("progress")]
        [ValidateProgressSchema]
        public async Task<IActionResult> UpdateProgress([FromBody] JObject rawRequest)
        {
            var request = rawRequest.ToObject<EPhytoProgressRequest>();

            if (request == null || request.DocumentControl == null)
            {
                return BadRequest(_response.CreateError("Invalid request format.", 400));
            }

            // Step 0: Intercept Duplicate DocumentNumber (As requested by auditor)
            if (await _submissionService.IsDocumentNumberDuplicateAsync(request.DocumentControl.DocumentNumber))
            {
                return Conflict(_response.CreateError($"Duplicate DocumentNumber found: '{request.DocumentControl.DocumentNumber}'. This document already exists in the system.", 409));
            }

            // Extract AppNickName from JWT
            var source = User.FindFirstValue("AppNickName") ?? "";
 
            // Step 1: Save Payload
            var payloadId = await _submissionService.SaveResponsePayloadAsync(rawRequest.ToString(Formatting.None), source, request.DocumentControl.ReferenceNumber);

            if (payloadId == 0)
            {
                return BadRequest(_response.CreateError("Failed to create response payload.", 400));
            }

            // Step 2: Queue for background processing
            _queue.Enqueue(payloadId, request, source);

            var successData = new 
            { 
                ReferenceNumber = request.DocumentControl.ReferenceNumber, 
                Status = request.DocumentControl.ResponseInfo.Status 
            };

            return Ok(_response.CreateSuccess(successData, "Successfully received ePhyto progress."));
        }
    }
}
