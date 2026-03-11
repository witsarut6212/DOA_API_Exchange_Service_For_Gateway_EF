using Microsoft.AspNetCore.Mvc;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;
using DOA_API_Exchange_Service_For_Gateway.Services;
using DOA_API_Exchange_Service_For_Gateway.Helpers;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json;
using DOA_API_Exchange_Service_For_Gateway.Models;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Claims;

namespace DOA_API_Exchange_Service_For_Gateway.Controllers
{
    [ApiController]
    [Route("")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class EPhytoController : ControllerBase
    {
        private readonly IEPhytoService _ePhytoService;
        private readonly IEPhytoSubmissionQueue _submissionQueue;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public EPhytoController(IEPhytoService ePhytoService, IEPhytoSubmissionQueue submissionQueue, IConfiguration configuration, IWebHostEnvironment env)
        {
            _ePhytoService = ePhytoService;
            _submissionQueue = submissionQueue;
            _configuration = configuration;
            _env = env;
        }

        [HttpPost("asw/normal")]
        public async Task<IActionResult> AswEPhytoNormal([FromBody] JObject rawRequest)
        {
            var title = _configuration["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";

            var validationResult = await ValidateRequest(rawRequest, "ASW-ePhytoNormalModel.json", title);
            if (validationResult != null) return validationResult;

            var request = rawRequest.ToObject<EPhytoRequest>();
            if (request == null || request.XcDocument == null)
            {
                return BadRequest(ResponseWriter.CreateError(title, "Invalid request format after schema validation.", 400));
            }

            // Check Duplicate
            if (await _ePhytoService.IsDocumentExists(request.XcDocument.DocType, request.XcDocument.StatusCode, request.XcDocument.DocId))
            {
                var data = new Dictionary<string, string> { { "doc_id", request.XcDocument.DocId } };
                return Conflict(ResponseWriter.CreateError(title, $"Document {request.XcDocument.DocId} is already exists.", 409, HttpContext.TraceIdentifier, HttpContext.Request.Path, data));
            }

            // Extract AppNickName from JWT
            var source = User.FindFirstValue("AppNickName") ?? "";
 
            // Step 1: Save Payload
            var systemOrigin = "ASW";
            var payloadId = await _ePhytoService.SaveEPhytoPayloadAsync(rawRequest.ToString(Formatting.None), source, systemOrigin, request.XcDocument?.DocId);
            if (payloadId == 0)
            {
                return StatusCode(500, ResponseWriter.CreateError(title, "Failed to save submission payload.", 500));
            }

            // Step 2: Enqueue → Background Service จะ process ต่อ
            _submissionQueue.Enqueue(payloadId, request, source, systemOrigin);

            var successData = new Dictionary<string, string> { { "doc_id", request.XcDocument.DocId } };
            return Ok(ResponseWriter.CreateSuccess(title, successData, "ได้รับข้อมูลเรียบร้อยแล้ว ระบบกำลังประมวลผล"));
        }

        [HttpPost("ippc/normal")]
        public async Task<IActionResult> IppcEPhytoNormal([FromBody] JObject rawRequest)
        {
            var title = _configuration["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";

            var validationResult = await ValidateRequest(rawRequest, "IPPCNormalModel.json", title);
            if (validationResult != null) return validationResult;

            var request = rawRequest.ToObject<EPhytoRequest>();
            if (request == null || request.XcDocument == null)
            {
                return BadRequest(ResponseWriter.CreateError(title, "Invalid request format after schema validation.", 400));
            }

            if (request.XcDocument.DocType != "851" || request.XcDocument.StatusCode != "70")
            {
                var validations = new List<ApiValidation> { new ApiValidation { Field = "xc_document", Description = "ippc/normal endpoint only accepts doc_type 851 and status_code 70." } };
                return UnprocessableEntity(ResponseWriter.CreateError(title, "One or more field validation failed.", 422, null, null, null, validations));
            }

            // Check Duplicate
            if (await _ePhytoService.IsDocumentExists(request.XcDocument.DocType, request.XcDocument.StatusCode, request.XcDocument.DocId))
            {
                var data = new Dictionary<string, string> { { "doc_id", request.XcDocument.DocId } };
                return Conflict(ResponseWriter.CreateError(title, $"Document {request.XcDocument.DocId} is already exists.", 409, HttpContext.TraceIdentifier, HttpContext.Request.Path, data));
            }

            // Extract AppNickName from JWT
            var source = User.FindFirstValue("AppNickName") ?? "";
 
            // Step 1: Save Payload
            var systemOrigin = "IPPC";
            var payloadId = await _ePhytoService.SaveEPhytoPayloadAsync(rawRequest.ToString(Formatting.None), source, systemOrigin, request.XcDocument?.DocId);
            if (payloadId == 0)
            {
                return StatusCode(500, ResponseWriter.CreateError(title, "Failed to save submission payload.", 500));
            }

            // Step 2: Enqueue → Background Service จะ process ต่อ
            _submissionQueue.Enqueue(payloadId, request, source, systemOrigin);

            var successData = new Dictionary<string, string> { { "doc_id", request.XcDocument.DocId } };
            return Ok(ResponseWriter.CreateSuccess(title, successData, "ได้รับข้อมูลเรียบร้อยแล้ว ระบบกำลังประมวลผล"));
        }


        [HttpPost("ippc/re-export")]
        public async Task<IActionResult> IppcEPhytoReexport([FromBody] JObject rawRequest)
        {
            var title = _configuration["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";

            var validationResult = await ValidateRequest(rawRequest, "IPPCReexportModel.json", title);
            if (validationResult != null) return validationResult;

            var request = rawRequest.ToObject<EPhytoRequest>();
            if (request == null || request.XcDocument == null)
            {
                return BadRequest(ResponseWriter.CreateError(title, "Invalid request format after schema validation.", 400));
            }

            if (request.XcDocument.DocType != "657")
            {
                var validations = new List<ApiValidation> { new ApiValidation { Field = "xc_document.doc_type", Description = "ippc/re-export endpoint only accepts doc_type 657." } };
                return UnprocessableEntity(ResponseWriter.CreateError(title, "One or more field validation failed.", 422, null, null, null, validations));
            }

            // Check Duplicate
            if (await _ePhytoService.IsDocumentExists(request.XcDocument.DocType, request.XcDocument.StatusCode, request.XcDocument.DocId))
            {
                var data = new Dictionary<string, string> { { "doc_id", request.XcDocument.DocId } };
                return Conflict(ResponseWriter.CreateError(title, $"Document {request.XcDocument.DocId} is already exists.", 409, HttpContext.TraceIdentifier, HttpContext.Request.Path, data));
            }

            // Extract AppNickName from JWT
            var source = User.FindFirstValue("AppNickName") ?? "";
 
            // Step 1: Save Payload
            var systemOrigin = "IPPC";
            var payloadId = await _ePhytoService.SaveEPhytoPayloadAsync(rawRequest.ToString(Formatting.None), source, systemOrigin, request.XcDocument?.DocId);
            if (payloadId == 0)
            {
                return StatusCode(500, ResponseWriter.CreateError(title, "Failed to save submission payload.", 500));
            }

            // Step 2: Enqueue → Background Service จะ process ต่อ
            _submissionQueue.Enqueue(payloadId, request, source, systemOrigin);

            var successData = new Dictionary<string, string> { { "doc_id", request.XcDocument.DocId } };
            return Ok(ResponseWriter.CreateSuccess(title, successData, "ได้รับข้อมูลเรียบร้อยแล้ว ระบบกำลังประมวลผล"));
        }

        [HttpPost("ippc/withdraw")]
        public async Task<IActionResult> IppcEPhytoToWithdraw([FromBody] JObject rawRequest)
        {
            var title = _configuration["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";

            var validationResult = await ValidateRequest(rawRequest, "IPPCWithdrawModel.json", title);
            if (validationResult != null) return validationResult;

            var request = rawRequest.ToObject<EPhytoRequest>();
            if (request == null || request.XcDocument == null)
            {
                return BadRequest(ResponseWriter.CreateError(title, "Invalid request format after schema validation.", 400));
            }

            if (request.XcDocument.DocType != "851" || request.XcDocument.StatusCode != "40")
            {
                var validations = new List<ApiValidation> { new ApiValidation { Field = "xc_document", Description = "ippc/withdraw endpoint only accepts doc_type 851 and status_code 40." } };
                return UnprocessableEntity(ResponseWriter.CreateError(title, "One or more field validation failed.", 422, null, null, null, validations));
            }

            // Check Duplicate
            if (await _ePhytoService.IsDocumentExists(request.XcDocument.DocType, request.XcDocument.StatusCode, request.XcDocument.DocId))
            {
                var data = new Dictionary<string, string> { { "doc_id", request.XcDocument.DocId } };
                return Conflict(ResponseWriter.CreateError(title, $"Document {request.XcDocument.DocId} is already exists.", 409, HttpContext.TraceIdentifier, HttpContext.Request.Path, data));
            }

            // Extract AppNickName from JWT
            var source = User.FindFirstValue("AppNickName") ?? "";
 
            // Step 1: Save Payload
            var systemOrigin = "IPPC";
            var payloadId = await _ePhytoService.SaveEPhytoPayloadAsync(rawRequest.ToString(Formatting.None), source, systemOrigin, request.XcDocument?.DocId);
            if (payloadId == 0)
            {
                return StatusCode(500, ResponseWriter.CreateError(title, "Failed to save submission payload.", 500));
            }

            // Step 2: Enqueue → Background Service จะ process ต่อ
            _submissionQueue.Enqueue(payloadId, request, source, systemOrigin);

            var successData = new Dictionary<string, string> { { "doc_id", request.XcDocument.DocId } };
            return Ok(ResponseWriter.CreateSuccess(title, successData, "ได้รับข้อมูลเรียบร้อยแล้ว ระบบกำลังประมวลผล"));
        }

        private async Task<IActionResult?> ValidateRequest(JObject rawRequest, string schemaFileName, string title)
        {
            var schemaPath = Path.Combine(_env.ContentRootPath, _configuration["Configuration.StoragePath"] ?? "Storage", "Schemas", schemaFileName);
            if (System.IO.File.Exists(schemaPath))
            {
                var schemaJson = await System.IO.File.ReadAllTextAsync(schemaPath);
                var schema = JSchema.Parse(schemaJson);

                if (!rawRequest.IsValid(schema, out IList<ValidationError> errors))
                {
                    var validations = new List<ApiValidation>();

                    Action<IEnumerable<ValidationError>> extractErrors = null!;
                    extractErrors = (errs) => {
                        foreach (var e in errs)
                        {
                            // ─── Special case: allOf+contains pattern ───────────────────────────────
                            // เมื่อ subject ใดขาดไป Newtonsoft จะสร้าง child errors สำหรับทุก item
                            // ในอาร์เรย์ที่ไม่ match (รวมถึง subject ที่มีอยู่แล้ว) ซึ่งทำให้ error
                            // เยอะเกินจริง → ดักไว้ที่นี่แล้วรายงาน error เดียวที่ระบุ subject ที่ขาด
                            if (e.ErrorType == ErrorType.Contains)
                            {
                                string? missingSubject = null;
                                try
                                {
                                    // ค้นหา const value ที่คาดหวัง จาก child errors ชั้นแรกหรือชั้นที่สอง
                                    var constError = e.ChildErrors
                                        .FirstOrDefault(c => c.ErrorType == ErrorType.Const && c.Schema?.Const != null);

                                    if (constError == null)
                                    {
                                        constError = e.ChildErrors
                                            .SelectMany(c => c.ChildErrors)
                                            .FirstOrDefault(c => c.ErrorType == ErrorType.Const && c.Schema?.Const != null);
                                    }

                                    missingSubject = constError?.Schema?.Const?.ToString();
                                }
                                catch { /* ignore — fallback to generic message */ }

                                var field = string.IsNullOrEmpty(e.Path) ? "include_notes" : e.Path;
                                var description = missingSubject != null
                                    ? $"Field subject '{missingSubject}' is required in include_notes."
                                    : "One or more required subjects are missing in include_notes.";

                                if (!validations.Any(v => v.Field == field && v.Description == description))
                                    validations.Add(new ApiValidation { Field = field, Description = description });

                                continue; // ไม่ recurse เข้า children
                            }

                            if (e.ChildErrors.Any())
                            {
                                extractErrors(e.ChildErrors);
                            }
                            else
                            {
                                var field = e.Path;
                                var description = e.Message;

                                if (e.ErrorType == ErrorType.Required)
                                {
                                    var missingProp = e.Message.Split(':').Last().Trim().Trim('.');
                                    field = string.IsNullOrEmpty(e.Path) ? missingProp : $"{e.Path}.{missingProp}";
                                    description = $"Field {missingProp} is required.";
                                }

                                if (!validations.Any(v => v.Field == field && v.Description == description))
                                {
                                    validations.Add(new ApiValidation { Field = field, Description = description });
                                }
                            }
                        }
                    };


                    extractErrors(errors);

                    return UnprocessableEntity(ResponseWriter.CreateError(title, "One or more field validation failed.", 422, null, null, null, validations));
                }
            }
            return null;
        }
    }
}
