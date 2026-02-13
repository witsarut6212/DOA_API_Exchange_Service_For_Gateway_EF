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
    [Microsoft.AspNetCore.Authorization.Authorize]
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
        public async Task<IActionResult> AswEPhytoNormal([FromBody] JObject jsonData) => await ProcessEPhytoSubmission(jsonData, "ASW");

        [HttpPost("IPPC-ePhytoNormal")]
        public async Task<IActionResult> IppcEPhytoNormal([FromBody] JObject jsonData) => await ProcessEPhytoSubmission(jsonData, "IPPC");

        [HttpPost("IPPC-ePhytoReexport")]
        public async Task<IActionResult> IppcEPhytoReexport([FromBody] JObject jsonData) => await ProcessEPhytoSubmission(jsonData, "IPPC_REEXPORT");

        [HttpPost("IPPC-ePhytoToWithdraw")]
        public async Task<IActionResult> IppcEPhytoToWithdraw([FromBody] JObject jsonData) => await ProcessEPhytoSubmission(jsonData, "IPPC_WITHDRAW");

        private async Task<IActionResult> ProcessEPhytoSubmission(JObject jsonData, string source)
        {
            try
            {
                string schemaPath = Path.Combine(_env.ContentRootPath, "Schemas", "ephyto_schema.json");
                if (System.IO.File.Exists(schemaPath))
                {
                    string schemaJson = await System.IO.File.ReadAllTextAsync(schemaPath);
                    JSchema schema = JSchema.Parse(schemaJson);
                    IList<string> errorMessages = new List<string>();
                    using (var reader = jsonData.CreateReader())
                    using (var validatingReader = new JSchemaValidatingReader(reader))
                    {
                        validatingReader.Schema = schema;
                        validatingReader.ValidationEventHandler += (o, a) => errorMessages.Add(a.Message);
                        while (validatingReader.Read()) { }
                    }
                    if (errorMessages.Count > 0) return BadRequest(new { status = "Validation Failed", errors = errorMessages });
                }

                var docData = jsonData["xc_document"] as JObject;
                var consignmentData = jsonData["consignment"] as JObject;
                var itemsData = jsonData["items"] as JArray;
                var phytoCertsData = jsonData["phytoCerts"] as JArray;

                if (docData == null || consignmentData == null || itemsData == null)
                    return BadRequest(new { message = "Missing core objects" });

                string msgId = Guid.NewGuid().ToString();
                string docId = SafeGet(docData, "doc_id") ?? "";
                string docType = SafeGet(docData, "doc_type") ?? "";
                string docStatus = SafeGet(docData, "status_code") ?? "";

                try
                {
                    if (await _context.TabMessageThphytos.AnyAsync(t => t.DocId == docId && t.DocType == docType && t.DocStatus == docStatus))
                        return Conflict(new { status = "Duplicate", message = $"Document {docId} exists." });
                }
                catch (Exception ex)
                {
                    if (ex.Message.ToLower().Contains("transient") || ex.Message.ToLower().Contains("connect") || ex.InnerException?.Message.ToLower().Contains("connect") == true)
                    {
                        return StatusCode(503, new { status = "Error", message = "Cannot connect to database" });
                    }
                    throw;
                }

                var thphyto = new TabMessageThphyto
                {
                    MessageId = msgId, MessageStatus = "NEW", PhytoTo = source,
                    DocName = SafeGet(docData, "doc_name"), DocId = docId, DocType = docType, DocStatus = docStatus,
                    IssueDateTime = DateTime.TryParse(SafeGet(docData, "issue_date"), out var dt) ? dt : DateTime.Now,
                    IssuerName = SafeGet(docData, "issue_party_name") ?? "N/A",
                    RequestDateTime = DateTime.Now,
                    
                    ConsignorName = SafeGet(docData["consignment"]?["consignor_party"], "name") ?? "N/A",
                    ConsignorAddrLine1 = SafeGet(docData["consignment"]?["consignor_party"], "adress_line1"),
                    ConsigneeName = SafeGet(docData["consignment"]?["consignee_party"], "name") ?? "N/A",
                    ConsigneeAddrLine1 = SafeGet(docData["consignment"]?["consignee_party"], "adress_line1"),

                    ExportCountryId = SafeGet(consignmentData["export_country"], "id") ?? SafeGet(consignmentData, "export_country_id") ?? "",
                    ImportCountryId = SafeGet(consignmentData["import_country"], "id") ?? SafeGet(consignmentData, "import_country_id") ?? "",
                    UnloadingBasePortName = SafeGet(consignmentData["unloading_baseport"], "name"),
                    
                    AuthLocationName = SafeGet(docData["signatory_authen"], "issue_location"),
                    AuthProviderName = SafeGet(docData["signatory_authen"]?["provider_party"], "name"),
                    AuthActualDateTime = SafeGet(docData["signatory_authen"], "actual_datetime"),

                    ResponseStatus = "0101", TimeStamp = DateTime.Now, LastUpdate = DateTime.Now, QueueStatus = "IN-QUEUE"
                };
                _context.TabMessageThphytos.Add(thphyto);

                if (docData["include_notes"] is JArray headerNotes) {
                    foreach (var hn in headerNotes) {
                        if (hn is JObject hnObj) {
                            var contents = hnObj["contents"] as JArray;
                            string contentStr = contents != null ? string.Join(", ", contents.Select(c => c is JObject ? SafeGet(c, "content") : c.ToString())) : "";
                            _context.TabMessageThphytoIncludedNotes.Add(new TabMessageThphytoIncludedNote { MessageId = msgId, Subject = SafeGet(hnObj, "subject") ?? "N/A", Content = contentStr, CreatedAt = DateTime.Now });
                        }
                    }
                }

                var allRefs = new List<JToken>();
                if (docData["reference_docs"] is JArray rArr) allRefs.AddRange(rArr);
                if (phytoCertsData != null) allRefs.AddRange(phytoCertsData);
                foreach (var r in allRefs) {
                    if (r is JObject rObj) {
                        _context.TabMessageThphytoReferenceDocs.Add(new TabMessageThphytoReferenceDoc {
                            MessageId = msgId, DocId = docId,
                            RefDocId = SafeGet(rObj, "doc_id") ?? SafeGet(rObj, "documentNo") ?? "",
                            Filename = SafeGet(rObj, "filename") ?? SafeGet(rObj, "Name"),
                            PdfObject = SafeGet(rObj, "PdfObject"), CreatedAt = DateTime.Now
                        });
                    }
                }

                if (consignmentData["utilize_transport"] is JObject utilObj) 
                    _context.TabMessageThphytoUtilizeTransports.Add(new TabMessageThphytoUtilizeTransport { MessageId = msgId, SealNumber = SafeGet(utilObj, "seal_number"), CreatedAt = DateTime.Now });
                
                if (consignmentData["main_carriages"] is JArray mcs) {
                    foreach (var mc in mcs) {
                        if (mc is JObject mcObj) _context.TabMessageThphytoMainCarriages.Add(new TabMessageThphytoMainCarriage { MessageId = msgId, TransportModeCode = SafeGet(mcObj, "mode_code"), TransportMeanName = SafeGet(mcObj, "transport_mean_name") ?? SafeGet(mcObj, "trasport_mean_name"), CreatedAt = DateTime.Now });
                    }
                }

                foreach (var jItm in itemsData) {
                    if (jItm is JObject itmObj) {
                        string itmId = Guid.NewGuid().ToString();
                        _context.TabMessageThphytoItems.Add(new TabMessageThphytoItem { MessageId = msgId, ItemId = itmId, SequenceNo = itmObj["sequence_no"]?.Value<int>() ?? 0, ProductScientName = SafeGet(itmObj, "scient_name"), CreatedAt = DateTime.Now });
                        
                        if (itmObj["descriptions"] is JArray ds) foreach (var d in ds) _context.TabMessageThphytoItemDescriptions.Add(new TabMessageThphytoItemDescription { MessageId = msgId, ItemId = itmId, ProductDescription = d is JObject ? SafeGet(d, "name") : d.ToString(), CreatedAt = DateTime.Now });
                        if (itmObj["common_names"] is JArray cs) foreach (var c in cs) _context.TabMessageThphytoItemCommonNames.Add(new TabMessageThphytoItemCommonName { MessageId = msgId, ItemId = itmId, ProudctCommonName = c is JObject ? SafeGet(c, "name") : c.ToString(), CreatedAt = DateTime.Now });
                        if (itmObj["additional_notes"] is JArray nts) {
                            foreach (var n in nts) {
                                if (n is JObject nObj) {
                                    string nid = Guid.NewGuid().ToString();
                                    _context.TabMessageThphytoItemAdditionalNotes.Add(new TabMessageThphytoItemAdditionalNote { MessageId = msgId, ItemId = itmId, AdditionalNoteId = nid, Subject = SafeGet(nObj, "subject") ?? "N/A", CreatedAt = DateTime.Now });
                                    var cnts = nObj["contents"] as JArray;
                                    if (cnts != null) foreach (var c in cnts) _context.TabMessageThphytoItemAdditionalNoteContents.Add(new TabMessageThphytoItemAdditionalNoteContent { MessageId = msgId, ItemId = itmId, AdditionalNoteId = nid, NoteContent = c is JObject ? SafeGet(c, "content") : c.ToString(), CreatedAt = DateTime.Now });
                                }
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { status = "Success", messageId = msgId, docId = docId });
            }
            catch (Exception ex)
            {
                if (ex.InnerException is MySqlConnector.MySqlException 
                    || ex is DbUpdateException 
                    || ex is Microsoft.EntityFrameworkCore.Storage.RetryLimitExceededException
                    || ex.Message.ToLower().Contains("connect")
                    || ex.Message.ToLower().Contains("access denied")
                    || ex.Message.ToLower().Contains("transient")
                    || (ex is InvalidOperationException && ex.Message.ToLower().Contains("resiliency")))
                {
                    throw; 
                }

                _logger.LogError(ex, "E-Phyto Submission Error");
                return StatusCode(500, new { 
                    status = "Error", 
                    message = ex.Message, 
                    details = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
        private string? SafeGet(JToken? token, string key) {
            if (token == null || token.Type == JTokenType.Null || !(token is JObject obj)) return null;
            return obj[key]?.ToString();
        }
        }
    }
}
