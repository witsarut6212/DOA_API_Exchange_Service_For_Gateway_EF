using Microsoft.AspNetCore.Mvc;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;
using DOA_API_Exchange_Service_For_Gateway.Services;
using DOA_API_Exchange_Service_For_Gateway.Helpers;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using DOA_API_Exchange_Service_For_Gateway.Models;

namespace DOA_API_Exchange_Service_For_Gateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class EPhytoController : ControllerBase
    {
        private readonly IEPhytoService _ePhytoService;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public EPhytoController(IEPhytoService ePhytoService, IConfiguration configuration, IWebHostEnvironment env)
        {
            _ePhytoService = ePhytoService;
            _configuration = configuration;
            _env = env;
        }

        [HttpPost("ASW-ePhytoNormal")]
        public async Task<IActionResult> AswEPhytoNormal([FromBody] JObject rawRequest)
        {
            var title = _configuration["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";

            // 1. JSON Schema Validation (Single Source of Truth)
            var schemaPath = Path.Combine(_env.ContentRootPath, "Schemas", "ASW-ePhytoNormalModel.json");
            if (System.IO.File.Exists(schemaPath))
            {
                var schemaJson = await System.IO.File.ReadAllTextAsync(schemaPath);
                var schema = JSchema.Parse(schemaJson);

                if (!rawRequest.IsValid(schema, out IList<ValidationError> errors))
                {
                    var validations = errors.Select(e =>
                    {
                        var field = e.Path;
                        var description = e.Message;

                        if (e.ErrorType == ErrorType.Required)
                        {
                            var missingProp = e.Message.Split(':').Last().Trim().Trim('.');
                            field = string.IsNullOrEmpty(e.Path) ? missingProp : $"{e.Path}.{missingProp}";
                            description = $"Field {missingProp} is required.";
                        }

                        return new ApiValidation
                        {
                            Field = field,
                            Description = description
                        };
                    }).ToList();

                    return UnprocessableEntity(ResponseWriter.CreateError(title, "One or more field validation failed.", 422, null, null, null, validations));
                }
            }

            var request = rawRequest.ToObject<EPhytoRequest>();
            if (request == null || request.XcDocument == null)
            {
                return BadRequest(ResponseWriter.CreateError(title, "Invalid request format after schema validation.", 400));
            }

            return await ProcessSubmission(request, "ASW");
        }

        [HttpPost("IPPC-ePhytoNormal")]
        public async Task<IActionResult> IppcEPhytoNormal([FromBody] JObject rawRequest)
        {
            var title = _configuration["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";
            
            // Validate using ASW schema structure (Normal)
            var schemaPath = Path.Combine(_env.ContentRootPath, "Schemas", "ASW-ePhytoNormalModel.json");
            if (System.IO.File.Exists(schemaPath))
            {
                var schemaJson = await System.IO.File.ReadAllTextAsync(schemaPath);
                var schema = JSchema.Parse(schemaJson);

                if (!rawRequest.IsValid(schema, out IList<ValidationError> errors))
                {
                    var validations = errors.Select(e => new ApiValidation { 
                        Field = e.ErrorType == ErrorType.Required ? (string.IsNullOrEmpty(e.Path) ? e.Message.Split(':').Last().Trim().Trim('.') : $"{e.Path}.{e.Message.Split(':').Last().Trim().Trim('.')}") : e.Path,
                        Description = e.ErrorType == ErrorType.Required ? $"Field {e.Message.Split(':').Last().Trim().Trim('.')} is required." : e.Message 
                    }).ToList();
                    return UnprocessableEntity(ResponseWriter.CreateError(title, "One or more field validation failed.", 422, null, null, null, validations));
                }
            }

            var request = rawRequest.ToObject<EPhytoRequest>();
            return await ProcessSubmission(request!, "IPPC");
        }

        [HttpPost("IPPC-ePhytoReexport")]
        public async Task<IActionResult> IppcEPhytoReexport([FromBody] JObject rawRequest)
        {
            var title = _configuration["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";
            
            // Using logic validation for Reexport (DocType 657)
            var request = rawRequest.ToObject<EPhytoRequest>();
            if (request?.XcDocument?.DocType != "657")
            {
                var validations = new List<ApiValidation> { new ApiValidation { Field = "xc_document.doc_type", Description = "Field doc_type must be 657 for Reexport." } };
                return UnprocessableEntity(ResponseWriter.CreateError(title, "One or more field validation failed.", 422, null, null, null, validations));
            }

            return await ProcessSubmission(request!, "IPPC_REEXPORT");
        }

        [HttpPost("IPPC-ePhytoToWithdraw")]
        public async Task<IActionResult> IppcEPhytoToWithdraw([FromBody] JObject rawRequest)
        {
            var title = _configuration["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";

            var request = rawRequest.ToObject<EPhytoRequest>();
            if (request?.XcDocument?.DocType != "851" || request?.XcDocument?.StatusCode != "40")
            {
                var validations = new List<ApiValidation> { new ApiValidation { Field = "xc_document", Description = "Invalid doc_type or status_code for Withdraw (851/40)." } };
                return UnprocessableEntity(ResponseWriter.CreateError(title, "One or more field validation failed.", 422, null, null, null, validations));
            }

            return await ProcessSubmission(request!, "IPPC_WITHDRAW");
        }

        private async Task<IActionResult> ProcessSubmission(EPhytoRequest request, string source)
        {
            var title = _configuration["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";

            if (await _ePhytoService.IsDocumentExists(request.XcDocument.DocId, request.XcDocument.DocType, request.XcDocument.StatusCode))
            {
                var data = new Dictionary<string, string> { { "doc_id", request.XcDocument.DocId } };
                return Conflict(ResponseWriter.CreateError(title, $"Document {request.XcDocument.DocId} is already exists.", 409, HttpContext.TraceIdentifier, HttpContext.Request.Path, data));
            }

            await _ePhytoService.SubmitEPhytoAsync(request, source);

            var successData = new Dictionary<string, string> { { "doc_id", request.XcDocument.DocId } };
            return Ok(ResponseWriter.CreateSuccess(title, successData, "Upload ไฟล์ด้วย Base64 สำเร็จ"));
        }
    }
}
