using Microsoft.AspNetCore.Mvc;
using DOA_API_Exchange_Service_For_Gateway.Models;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;
using DOA_API_Exchange_Service_For_Gateway.Services;

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
        public async Task<IActionResult> AswEPhytoNormal([FromBody] EPhytoRequest request) => await ProcessSubmission(request, "ASW");

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
                return Conflict(new ApiResponse<object>
                {
                    Info = new ApiInfo
                    {
                        Title = title,
                        Detail = $"Document {request.XcDocument.DocId} is already exists.",
                        SystemCode = 409
                    },
                    Data = new Dictionary<string, string>
                    {
                        { "doc_id", request.XcDocument.DocId }
                    },
                    Error = new ApiError
                    {
                        TraceId = HttpContext.TraceIdentifier,
                        Instance = HttpContext.Request.Path
                    }
                });
            }

            await _ePhytoService.SubmitEPhytoAsync(request, source);

            return Ok(new ApiResponse<object>
            {
                Info = new ApiInfo
                {
                    Title = title,
                    Detail = "Upload ไฟล์ด้วย Base64 สำเร็จ",
                    SystemCode = 200
                },
                Data = new Dictionary<string, string>
                {
                    { "doc_id", request.XcDocument.DocId }
                }
            });
        }
    }
}
