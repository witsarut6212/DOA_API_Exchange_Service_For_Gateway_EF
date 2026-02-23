using Microsoft.AspNetCore.Mvc;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;
using DOA_API_Exchange_Service_For_Gateway.Services;
using DOA_API_Exchange_Service_For_Gateway.Helpers;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using DOA_API_Exchange_Service_For_Gateway.Models;
using System.Collections.Generic;
using System.Linq;
using System.IO;

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

            var validationResult = await ValidateRequest(rawRequest, "ASW-ePhytoNormalModel.json", title);
            if (validationResult != null) return validationResult;

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

            var validationResult = await ValidateRequest(rawRequest, "IPPCNormalReexportWithdrawModel.json", title);
            if (validationResult != null) return validationResult;

            var request = rawRequest.ToObject<EPhytoRequest>();

            if (request?.XcDocument?.DocType != "851" || request?.XcDocument?.StatusCode != "70")
            {
                var validations = new List<ApiValidation> { new ApiValidation { Field = "xc_document", Description = "IPPC-ePhytoNormal endpoint only accepts doc_type 851 and status_code 70." } };
                return UnprocessableEntity(ResponseWriter.CreateError(title, "One or more field validation failed.", 422, null, null, null, validations));
            }

            return await ProcessSubmission(request!, "IPPC");
        }

        [HttpPost("IPPC-ePhytoReexport")]
        public async Task<IActionResult> IppcEPhytoReexport([FromBody] JObject rawRequest)
        {
            var title = _configuration["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";

            var validationResult = await ValidateRequest(rawRequest, "IPPCNormalReexportWithdrawModel.json", title);
            if (validationResult != null) return validationResult;

            var request = rawRequest.ToObject<EPhytoRequest>();

            if (request?.XcDocument?.DocType != "657")
            {
                var validations = new List<ApiValidation> { new ApiValidation { Field = "xc_document.doc_type", Description = "IPPC-ePhytoReexport endpoint only accepts doc_type 657." } };
                return UnprocessableEntity(ResponseWriter.CreateError(title, "One or more field validation failed.", 422, null, null, null, validations));
            }

            return await ProcessSubmission(request!, "IPPC_REEXPORT");
        }

        [HttpPost("IPPC-ePhytoToWithdraw")]
        public async Task<IActionResult> IppcEPhytoToWithdraw([FromBody] JObject rawRequest)
        {
            var title = _configuration["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";

            var validationResult = await ValidateRequest(rawRequest, "IPPCNormalReexportWithdrawModel.json", title);
            if (validationResult != null) return validationResult;

            var request = rawRequest.ToObject<EPhytoRequest>();

            if (request?.XcDocument?.DocType != "851" || request?.XcDocument?.StatusCode != "40")
            {
                var validations = new List<ApiValidation> { new ApiValidation { Field = "xc_document", Description = "IPPC-ePhytoToWithdraw endpoint only accepts doc_type 851 and status_code 40." } };
                return UnprocessableEntity(ResponseWriter.CreateError(title, "One or more field validation failed.", 422, null, null, null, validations));
            }

            return await ProcessSubmission(request!, "IPPC_WITHDRAW");
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

        private async Task<IActionResult> ProcessSubmission(EPhytoRequest request, string source)
        {
            var title = _configuration["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";

            // เก็บ doc_id ไว้ใน HttpContext.Items เผื่อ Middleware ต้องใช้ใน log
            HttpContext.Items["doc_id"] = request.XcDocument.DocId;

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
