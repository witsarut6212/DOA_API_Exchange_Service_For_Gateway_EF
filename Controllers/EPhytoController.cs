using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using DOA_API_Exchange_Service_For_Gateway.Models;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;
using DOA_API_Exchange_Service_For_Gateway.Services;
using DOA_API_Exchange_Service_For_Gateway.Helpers;

namespace DOA_API_Exchange_Service_For_Gateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class EPhytoController : ControllerBase
    {
        private readonly IEPhytoService _ePhytoService;
        private readonly IConfiguration _configuration;

        public EPhytoController(IEPhytoService ePhytoService, IConfiguration configuration)
        {
            _ePhytoService = ePhytoService;
            _configuration = configuration;
        }

        [HttpPost("ASW-ePhytoNormal")]
        public async Task<IActionResult> AswEPhytoNormal([FromBody] EPhytoRequest request)
        {
            var doc = request.XcDocument;
            bool isValid = (doc.DocType == "851" && doc.StatusCode == "70");

            if (!isValid)
            {
                var title = _configuration["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";
                return BadRequest(ResponseWriter.CreateError(title, "Invalid doc_type or status_code for ASW normal.", 400, HttpContext.TraceIdentifier, HttpContext.Request.Path));
            }

            return await ProcessSubmission(request, "ASW");
        }

        [HttpPost("IPPC-ePhytoNormal")]
        public async Task<IActionResult> IppcEPhytoNormal([FromBody] EPhytoRequest request) => await ProcessSubmission(request, "IPPC");

        [HttpPost("IPPC-ePhytoReexport")]
        public async Task<IActionResult> IppcEPhytoReexport([FromBody] EPhytoRequest request) => await ProcessSubmission(request, "IPPC_REEXPORT");

        [HttpPost("IPPC-ePhytoToWithdraw")]
        public async Task<IActionResult> IppcEPhytoToWithdraw([FromBody] EPhytoRequest request) => await ProcessSubmission(request, "IPPC_WITHDRAW");

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
