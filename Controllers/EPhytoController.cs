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
        public async Task<IActionResult> AswEPhytoNormal([FromBody] JObject jsonData) => await ProcessEPhytoSubmission(jsonData, "ASW");

        [HttpPost("IPPC-ePhytoNormal")]
        public async Task<IActionResult> IppcEPhytoNormal([FromBody] JObject jsonData) => await ProcessEPhytoSubmission(jsonData, "IPPC");

        [HttpPost("IPPC-ePhytoReexport")]
        public async Task<IActionResult> IppcEPhytoReexport([FromBody] JObject jsonData) => await ProcessEPhytoSubmission(jsonData, "IPPC_REEXPORT");

        private async Task<IActionResult> ProcessEPhytoSubmission(JObject jsonData, string source)
        {
            try
            {
                // Step 1: Validation (Fixed Build Error)
                string schemaPath = Path.Combine(_env.ContentRootPath, "Schemas", "ephyto_schema.json");
                if (System.IO.File.Exists(schemaPath))
                {
                    string schemaJson = await System.IO.File.ReadAllTextAsync(schemaPath);
                    JSchema schema = JSchema.Parse(schemaJson);
                    
                    // ประกาศตัวแปรแยกเพื่อให้ Compiler ไม่สับสน
                    IList<string> errorMessages;
                    if (!jsonData.IsValid(schema, out errorMessages))
                        return BadRequest(new { status = "Validation Failed", errors = errorMessages });
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

                if (await _context.TabMessageThphytos.AnyAsync(t => t.DocId == docId && t.DocType == docType && t.DocStatus == docStatus))
                    return Conflict(new { status = "Duplicate", message = $"Document {docId} exists." });

                // Step 3: Main Document Mapping
                var thphyto = new TabMessageThphyto
                {
                    MessageId = msgId, MessageStatus = "NEW", PhytoTo = source,
                    DocName = docData["doc_name"]?.ToString(), DocId = docId, DocType = docType, DocStatus = docStatus,
                    IssueDateTime = DateTime.TryParse(docData["issue_date"]?.ToString(), out var dt) ? dt : DateTime.Now,
                    IssuerName = docData["issue_party_name"]?.ToString() ?? "N/A",
                    RequestDateTime = DateTime.Now,
                    
                    ConsignorName = consignmentData["consignor_party"]?["name"]?.ToString() ?? "N/A",
                    ConsignorAddrLine1 = consignmentData["consignor_party"]?["adress_line1"]?.ToString(),
                    ConsignorAddrLine2 = (consignmentData["consignor_party"]?["addres_line2"] ?? consignmentData["consignor_party"]?["adress_line2"])?.ToString(),
                    
                    ConsigneeName = consignmentData["consignee_party"]?["name"]?.ToString() ?? "N/A",
                    ConsigneeAddrLine1 = consignmentData["consignee_party"]?["adress_line1"]?.ToString(),
                    ConsigneeAddrLine2 = (consignmentData["consignee_party"]?["addres_line2"] ?? consignmentData["consignee_party"]?["adress_line2"])?.ToString(),

                    ExportCountryId = (consignmentData["export_country"]?["id"] ?? consignmentData["export_country_id"])?.ToString() ?? "",
                    ImportCountryId = (consignmentData["import_country"]?["id"] ?? consignmentData["import_country_id"])?.ToString() ?? "",
                    UnloadingBasePortName = consignmentData["unloading_baseport"]?["name"]?.ToString(),
                    
                    AuthLocationName = docData["signatory_authen"]?["issue_location"]?.ToString(),
                    AuthProviderName = docData["signatory_authen"]?["provider_party"]?["name"]?.ToString(),
                    AuthActualDateTime = docData["signatory_authen"]?["actual_datetime"]?.ToString(),

                    ResponseStatus = "0101", TimeStamp = DateTime.Now, LastUpdate = DateTime.Now, QueueStatus = "IN-QUEUE"
                };
                _context.TabMessageThphytos.Add(thphyto);

                // Step 4: Map Collections
                
                // Reference Documents & PDF
                var allRefs = new List<JToken>();
                if (docData["reference_docs"] is JArray rArr) allRefs.AddRange(rArr);
                if (phytoCertsData != null) allRefs.AddRange(phytoCertsData);
                foreach (var r in allRefs) {
                    _context.TabMessageThphytoReferenceDocs.Add(new TabMessageThphytoReferenceDoc {
                        MessageId = msgId, DocId = docId,
                        RefDocId = (r["doc_id"] ?? r["documentNo"])?.ToString() ?? "",
                        Filename = (r["filename"] ?? r["Name"])?.ToString(),
                        PdfObject = r["PdfObject"]?.ToString(), CreatedAt = DateTime.Now
                    });
                }

                // Extra Header Info
                if (consignmentData["utilize_transport"] is JObject u) 
                    _context.TabMessageThphytoUtilizeTransports.Add(new TabMessageThphytoUtilizeTransport { MessageId = msgId, SealNumber = u["seal_number"]?.ToString(), CreatedAt = DateTime.Now });
                if (consignmentData["main_carriages"] is JArray mcs)
                    foreach (var mc in mcs) _context.TabMessageThphytoMainCarriages.Add(new TabMessageThphytoMainCarriage { MessageId = msgId, TransportModeCode = mc["mode_code"]?.ToString(), TransportMeanName = (mc["transport_mean_name"] ?? mc["trasport_mean_name"])?.ToString(), CreatedAt = DateTime.Now });

                // Items Loop (With child value protection)
                foreach (var jItm in itemsData) {
                    string itmId = Guid.NewGuid().ToString();
                    _context.TabMessageThphytoItems.Add(new TabMessageThphytoItem {
                        MessageId = msgId, ItemId = itmId, SequenceNo = jItm["sequence_no"]?.Value<int>() ?? 0, ProductScientName = jItm["scient_name"]?.ToString(), CreatedAt = DateTime.Now
                    });

                    // Protective Mapping and Saving
                    if (jItm["descriptions"] is JArray ds) foreach (var d in ds) _context.TabMessageThphytoItemDescriptions.Add(new TabMessageThphytoItemDescription { MessageId = msgId, ItemId = itmId, ProductDescription = d is JObject ? d["name"]?.ToString() : d.ToString(), CreatedAt = DateTime.Now });
                    if (jItm["common_names"] is JArray cs) foreach (var c in cs) _context.TabMessageThphytoItemCommonNames.Add(new TabMessageThphytoItemCommonName { MessageId = msgId, ItemId = itmId, ProudctCommonName = c is JObject ? c["name"]?.ToString() : c.ToString(), CreatedAt = DateTime.Now });
                    
                    if (jItm["origin_countries"] is JArray ogns) 
                        foreach (var o in ogns) _context.TabMessageThphytoItemOriginCountries.Add(new TabMessageThphytoItemOriginCountry { MessageId = msgId, ItemId = itmId, CountryId = o is JObject ? o["id"]?.ToString() ?? "" : o.ToString(), CountryName = o is JObject ? o["name"]?.ToString() : null, CreatedAt = DateTime.Now });

                    // HS Code Protect
                    if (jItm["applicable_classifications"] is JArray cls) {
                        foreach (var c in cls) {
                            var names = c["class_names"] as JArray;
                            _context.TabMessageThphytoItemApplicableClassifications.Add(new TabMessageThphytoItemApplicableClassification {
                                MessageId = msgId, ItemId = itmId, ApplicableId = Guid.NewGuid().ToString(), SystemName = c["system_name"]?.ToString(), ClassCode = c["class_code"]?.ToString(),
                                ClassName = names != null ? string.Join(", ", names.Select(x => x is JObject ? x["class_name"]?.ToString() : x.ToString())) : "", CreatedAt = DateTime.Now
                            });
                        }
                    }

                    // Addtional Notes (Re-export 핵심)
                    if (jItm["additional_notes"] is JArray nts) {
                        foreach (var n in nts) {
                            string nid = Guid.NewGuid().ToString();
                            _context.TabMessageThphytoItemAdditionalNotes.Add(new TabMessageThphytoItemAdditionalNote { MessageId = msgId, ItemId = itmId, AdditionalNoteId = nid, Subject = n["subject"]?.ToString() ?? "N/A", CreatedAt = DateTime.Now });
                            
                            var contents = n["contents"] as JArray;
                            if (contents != null) {
                                foreach (var c in contents) _context.TabMessageThphytoItemAdditionalNoteContents.Add(new TabMessageThphytoItemAdditionalNoteContent { MessageId = msgId, ItemId = itmId, AdditionalNoteId = nid, NoteContent = c is JObject ? c["content"]?.ToString() : c.ToString(), CreatedAt = DateTime.Now });
                            } else if (n["content"] != null) {
                                _context.TabMessageThphytoItemAdditionalNoteContents.Add(new TabMessageThphytoItemAdditionalNoteContent { MessageId = msgId, ItemId = itmId, AdditionalNoteId = nid, NoteContent = n["content"]?.ToString(), CreatedAt = DateTime.Now });
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { status = "Success", messageId = msgId, docId = docId });
            }
            catch (Exception ex) {
                _logger.LogError(ex, "E-Phyto Submission Error");
                return StatusCode(500, new { status = "Error", message = ex.Message, details = ex.InnerException?.Message });
            }
        }

        [HttpGet("document/{docId}")]
        public async Task<IActionResult> GetDocument(string docId) {
            var doc = await _context.TabMessageThphytos.FirstOrDefaultAsync(d => d.DocId == docId);
            return doc != null ? Ok(doc) : NotFound();
        }
    }
}
