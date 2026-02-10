using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using DOA_API_Exchange_Service_For_Gateway.Data;
using DOA_API_Exchange_Service_For_Gateway.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DOA_API_Exchange_Service_For_Gateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EPhytoController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<EPhytoController> _logger;

        public EPhytoController(AppDbContext context, IWebHostEnvironment env, ILogger<EPhytoController> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        [HttpPost("ASW-ePhytoNormal")]
        public async Task<IActionResult> AswEPhytoNormal([FromBody] JObject jsonData)
        {
            return await ProcessEPhytoSubmission(jsonData, "ASW");
        }

        [HttpPost("IPPC-ePhytoNormal")]
        public async Task<IActionResult> IppcEPhytoNormal([FromBody] JObject jsonData)
        {
            return await ProcessEPhytoSubmission(jsonData, "IPPC");
        }

        private async Task<IActionResult> ProcessEPhytoSubmission(JObject jsonData, string source)
        {
            try
            {
                // Step 1: Validation
                string schemaPath = Path.Combine(_env.ContentRootPath, "Schemas", "ephyto_schema.json");
                if (System.IO.File.Exists(schemaPath))
                {
                    string schemaJson = await System.IO.File.ReadAllTextAsync(schemaPath);
                    JSchema schema = JSchema.Parse(schemaJson);
                    if (!jsonData.IsValid(schema, out IList<string> errorMessages))
                    {
                        return BadRequest(new { status = "Validation Failed", errors = errorMessages });
                    }
                }

                // Step 2: Read Data
                var docData = jsonData["xc_document"] as JObject;
                var consignmentData = jsonData["consignment"] as JObject;
                var itemsData = jsonData["items"] as JArray;
                var phytoCertsData = jsonData["phytoCerts"] as JArray;

                if (docData == null || consignmentData == null || itemsData == null)
                    return BadRequest(new { message = "Missing core objects" });

                string msgId = Guid.NewGuid().ToString();
                string docId = docData["doc_id"]?.ToString() ?? "";
                string docType = docData["doc_type"]?.ToString() ?? "";
                string docStatus = docData["status_code"]?.ToString() ?? "";

                // Duplicate Check
                if (await _context.TabMessageThphytos.AnyAsync(t => t.DocId == docId && t.DocType == docType && t.DocStatus == docStatus))
                    return Conflict(new { status = "Duplicate", message = $"Document {docId} exists." });

                // Step 3: Mapping thphyto (คืนค่าที่ห้ามเป็น null กลับมาทั้งหมด)
                var thphyto = new TabMessageThphyto
                {
                    MessageId = msgId, MessageStatus = "NEW", PhytoTo = source,
                    DocName = docData["doc_name"]?.ToString(), DocId = docId, DocType = docType, DocStatus = docStatus,
                    IssueDateTime = DateTime.TryParse(docData["issue_date"]?.ToString(), out var dt) ? dt : DateTime.Now,
                    IssuerName = docData["issue_party_name"]?.ToString() ?? "",
                    RequestDateTime = DateTime.Now,
                    
                    // Consignor
                    ConsignorName = consignmentData["consignor_party"]?["name"]?.ToString() ?? "N/A",
                    ConsignorAddrLine1 = consignmentData["consignor_party"]?["adress_line1"]?.ToString(),
                    ConsignorAddrLine2 = consignmentData["consignor_party"]?["addres_line2"]?.ToString() ?? consignmentData["consignor_party"]?["adress_line2"]?.ToString(),
                    
                    // Consignee
                    ConsigneeName = consignmentData["consignee_party"]?["name"]?.ToString() ?? "N/A",
                    ConsigneeAddrLine1 = consignmentData["consignee_party"]?["adress_line1"]?.ToString(),
                    ConsigneeAddrLine2 = consignmentData["consignee_party"]?["addres_line2"]?.ToString() ?? consignmentData["consignee_party"]?["adress_line2"]?.ToString(),

                    // Countries & Ports
                    ExportCountryId = consignmentData["export_country"]?["id"]?.ToString() ?? consignmentData["export_country_id"]?.ToString() ?? "",
                    ImportCountryId = consignmentData["import_country"]?["id"]?.ToString() ?? consignmentData["import_country_id"]?.ToString() ?? "",
                    UnloadingBasePortName = consignmentData["unloading_baseport"]?["name"]?.ToString(),
                    
                    // Auth info
                    AuthLocationName = docData["signatory_authen"]?["issue_location"]?.ToString(),
                    AuthProviderName = docData["signatory_authen"]?["provider_party"]?["name"]?.ToString(),
                    AuthActualDateTime = docData["signatory_authen"]?["actual_datetime"]?.ToString(),

                    ResponseStatus = "0101", TimeStamp = DateTime.Now, LastUpdate = DateTime.Now, MarkSendAsw = "N", MarkSendIppc = "N", QueueStatus = "IN-QUEUE"
                };
                _context.TabMessageThphytos.Add(thphyto);

                // Step 4: Related Lists
                if (docData["reference_docs"] is JArray rDocs) {
                    foreach (var rd in rDocs) _context.TabMessageThphytoReferenceDocs.Add(new TabMessageThphytoReferenceDoc { MessageId = msgId, DocId = docId, RefDocId = rd["doc_id"]?.ToString() ?? "", CreatedAt = DateTime.Now });
                }
                if (phytoCertsData != null) {
                    foreach (var pc in phytoCertsData) _context.TabMessageThphytoReferenceDocs.Add(new TabMessageThphytoReferenceDoc { MessageId = msgId, DocId = docId, RefDocId = pc["documentNo"]?.ToString() ?? "", PdfObject = pc["PdfObject"]?.ToString(), CreatedAt = DateTime.Now });
                }

                // Items Mapping
                foreach (var jItm in itemsData) {
                    string itmId = Guid.NewGuid().ToString();
                    _context.TabMessageThphytoItems.Add(new TabMessageThphytoItem { MessageId = msgId, ItemId = itmId, SequenceNo = jItm["sequence_no"]?.Value<int>() ?? 0, ProductScientName = jItm["scient_name"]?.ToString(), CreatedAt = DateTime.Now });
                    if (jItm["descriptions"] is JArray ds) foreach (var d in ds) _context.TabMessageThphytoItemDescriptions.Add(new TabMessageThphytoItemDescription { MessageId = msgId, ItemId = itmId, ProductDescription = d["name"]?.ToString(), CreatedAt = DateTime.Now });
                    if (jItm["common_names"] is JArray cn) foreach (var c in cn) _context.TabMessageThphytoItemCommonNames.Add(new TabMessageThphytoItemCommonName { MessageId = msgId, ItemId = itmId, ProudctCommonName = c["name"]?.ToString(), CreatedAt = DateTime.Now });
                }

                await _context.SaveChangesAsync();
                return Ok(new { status = "Success", messageId = msgId, docId = docId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Submit Error");
                return StatusCode(500, new { status = "Error", message = ex.Message, details = ex.InnerException?.Message });
            }
        }

        [HttpGet("document/{docId}")]
        public async Task<IActionResult> GetDocument(string docId)
        {
            var document = await _context.TabMessageThphytos.FirstOrDefaultAsync(d => d.DocId == docId);
            return document != null ? Ok(document) : NotFound();
        }
    }
}
