using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Linq;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;
using DOA_API_Exchange_Service_For_Gateway.Services;
using DOA_API_Exchange_Service_For_Gateway.Helpers;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

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

                if (!rawRequest.IsValid(schema, out IList<string> errors))
                {
                    var validationData = errors.Select(e => new { message = e }).ToList();
                    return UnprocessableEntity(ResponseWriter.CreateError(title, "JSON Schema validation failed for ASW Normal.", 422, HttpContext.TraceIdentifier, HttpContext.Request.Path, validationData));
                }
            }

            // 2. Map to Object
            var request = rawRequest.ToObject<EPhytoRequest>();
            if (request == null || request.XcDocument == null)
            {
                return BadRequest(ResponseWriter.CreateError(title, "Invalid request format after schema validation.", 400));
            }

            // Check for duplicates and process
            return await ProcessSubmission(request, "ASW");
        }

        [HttpPost("IPPC-ePhytoNormal")]
        public async Task<IActionResult> IppcEPhytoNormal([FromBody] EPhytoRequest request)
        {
            var doc = request.XcDocument;
            bool isValid = (doc.DocType == "851" && doc.StatusCode == "70");

            if (!isValid)
            {
                var title = _configuration["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";
                return BadRequest(ResponseWriter.CreateError(title, "Invalid doc_type or status_code for IPPC normal (85.", 400, HttpContext.TraceIdentifier, HttpContext.Request.Path));
            }

            return await ProcessSubmission(request, "IPPC");
        }

        [HttpPost("IPPC-ePhytoReexport")]
        public async Task<IActionResult> IppcEPhytoReexport([FromBody] EPhytoRequest request)
        {
            var doc = request.XcDocument;
            bool isValid = (doc.DocType == "657");

            if (!isValid)
            {
                var title = _configuration["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";
                return BadRequest(ResponseWriter.CreateError(title, "Invalid doc_type for IPPC Reexport.", 400, HttpContext.TraceIdentifier, HttpContext.Request.Path));
            }

            return await ProcessSubmission(request, "IPPC_REEXPORT");
        }

        [HttpPost("IPPC-ePhytoToWithdraw")]
        public async Task<IActionResult> IppcEPhytoToWithdraw([FromBody] EPhytoRequest request)
        {
            var doc = request.XcDocument;
            bool isValid = (doc.DocType == "851" && doc.StatusCode == "40");

            if (!isValid)
            {
                var title = _configuration["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";
                return BadRequest(ResponseWriter.CreateError(title, "Invalid doc_type or status_code for IPPC Withdraw.", 400, HttpContext.TraceIdentifier, HttpContext.Request.Path));
            }

            return await ProcessSubmission(request, "IPPC_WITHDRAW");
        }

        private async Task<IActionResult> ProcessSubmission(EPhytoRequest request, string source)
        {
            var title = _configuration["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";

            // Check if document already exists
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
