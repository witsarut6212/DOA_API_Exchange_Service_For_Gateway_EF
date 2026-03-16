using DOA_API_Exchange_Service_For_Gateway.Models.Requests;
using DOA_API_Exchange_Service_For_Gateway.Services;
using DOA_API_Exchange_Service_For_Gateway.Filters;
using DOA_API_Exchange_Service_For_Gateway.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Security.Claims;
using DOA_API_Exchange_Service_For_Gateway.Models;

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

        [HttpPost("certificate")]
        [ValidateCertificateSchema]
        public async Task<IActionResult> SubmitCertificate([FromBody] JObject rawRequest)
        {
            var request = rawRequest.ToObject<EPhytoCertificateRequest>();
            
            if (request == null || request.DocumentControl == null)
            {
                return BadRequest(_response.CreateError("Invalid request format.", 400));
            }

            var docControl = request.DocumentControl;
            var referenceNumber = docControl.ReferenceNumber;

            if (string.IsNullOrWhiteSpace(referenceNumber))
            {
                return BadRequest(_response.CreateError("ReferenceNumber is required.", 400));
            }

            // Step 0: Business Rule Validation
            if (!string.Equals(docControl.CertificateStatus, "Draft", StringComparison.OrdinalIgnoreCase))
            {
                var validations = new List<ApiValidation>
                {
                    new ApiValidation
                    {
                        Field = "DocumentControl.CertificateStatus",
                        Description = "Current implementation only supports 'Draft' status for submission."
                    }
                };
                return UnprocessableEntity(_response.CreateError("One or more field validation failed.", 422, null, validations));
            }

            // Optional: Check if referenceNumber already exists and is not editable (as per existing service logic)
            if (!await _submissionService.CanEditCertificateAsync(referenceNumber))
            {
                return Conflict(_response.CreateError($"ReferenceNumber '{referenceNumber}' already exists and cannot be modified in its current state.", 409));
            }

            var source = User.FindFirstValue("AppNickName") ?? string.Empty;

            // Determine if it's a PQ Certificate or ePhyto Certificate based on FormType
            var formType = docControl.FormType?.ToLower();
            bool isPq = formType == "pq7" || formType == "pq8" || formType == "pq9";
            
            int payloadId;
            string docTypeLabel;

            // Step 1: Save Payload and Outbound Txn
            if (isPq)
            {
                payloadId = await _submissionService.SaveEPhytoCertificatePayloadAsync(
                    rawRequest.ToString(Formatting.None),
                    source,
                    referenceNumber);
                docTypeLabel = "PQ certificate";
            }
            else
            {
                payloadId = await _submissionService.SaveCertificatePayloadAsync(
                    rawRequest.ToString(Formatting.None),
                    source,
                    referenceNumber);
                docTypeLabel = "ePhyto certificate";
            }

            if (payloadId == 0)
            {
                return StatusCode(500, _response.CreateError($"Failed to save {docTypeLabel} submission payload.", 500));
            }

            var successData = new
            {
                ReferenceNumber = referenceNumber
            };

            return Ok(_response.CreateSuccess(successData, $"Successfully received {docTypeLabel} submission."));
        }
    }
}
