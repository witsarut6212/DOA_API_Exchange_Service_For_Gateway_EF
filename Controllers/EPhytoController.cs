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
            try
            {
                // Step 1: JSON Schema Validation (Newtonsoft)
                string schemaPath = Path.Combine(_env.ContentRootPath, "Schemas", "ephyto_schema.json");
                
                if (System.IO.File.Exists(schemaPath))
                {
                    string schemaJson = await System.IO.File.ReadAllTextAsync(schemaPath);
                    JSchema schema = JSchema.Parse(schemaJson);

                    IList<string> errorMessages;
                    bool isValid = jsonData.IsValid(schema, out errorMessages);

                    if (!isValid)
                    {
                        return BadRequest(new
                        {
                            status = "Validation Failed",
                            errors = errorMessages
                        });
                    }
                }

                // Step 2: Extract data from JSON
                var docData = jsonData["xc_document"] as JObject;
                var consignmentData = jsonData["consignment"] as JObject;
                var itemsData = jsonData["items"] as JArray;

                if (docData == null || consignmentData == null || itemsData == null)
                {
                    return BadRequest(new { message = "Missing required objects: xc_document, consignment, or items" });
                }

                // Generate unique message ID string
                string messageIdStr = Guid.NewGuid().ToString();

                // Step 3: Map to TabMessageThphyto (Main Document)
                var thphytoEntity = new TabMessageThphyto
                {
                    MessageId = messageIdStr,
                    MessageStatus = "NEW",
                    PhytoTo = "ASW",
                    DocName = docData["doc_name"]?.ToString(),
                    DocId = docData["doc_id"]?.ToString() ?? "",
                    DocDescription = docData["doc_description"]?.ToString(),
                    DocType = docData["doc_type"]?.ToString() ?? "",
                    DocStatus = docData["status_code"]?.ToString() ?? "",
                    IssueDateTime = DateTime.TryParse(docData["issue_date"]?.ToString(), out var issueDate) 
                        ? issueDate : DateTime.Now,
                    IssuerId = docData["issue_party_id"]?.ToString(),
                    IssuerName = docData["issue_party_name"]?.ToString() ?? "",
                    RequestDateTime = DateTime.Now,
                    
                    // Signatory Authentication
                    AuthActualDateTime = docData["signatory_authen"]?["actual_datetime"]?.ToString(),
                    AuthLocationName = docData["signatory_authen"]?["issue_location"] is JValue 
                        ? docData["signatory_authen"]?["issue_location"]?.ToString()
                        : docData["signatory_authen"]?["issue_location"]?["name"]?.ToString(),
                    AuthProviderName = docData["signatory_authen"]?["provider_party"]?["specfied_person_name"]?.ToString()
                        ?? docData["signatory_authen"]?["provider_party"]?["name"]?.ToString(),
                    AuthSpecifyPersonName = docData["signatory_authen"]?["provider_party"]?["specfied_person_name"]?.ToString(),
                    
                    // Consignor mapping
                    ConsignorName = consignmentData["consignor_party"]?["name"]?.ToString() ?? "",
                    ConsignorAddrLine1 = consignmentData["consignor_party"]?["adress_line1"]?.ToString(),
                    ConsignorAddrLine2 = consignmentData["consignor_party"]?["adress_line2"]?.ToString(),
                    ConsignorAddrLine3 = consignmentData["consignor_party"]?["adress_line3"]?.ToString(),
                    ConsignorAddrLine4 = consignmentData["consignor_party"]?["adress_line4"]?.ToString(),
                    ConsignorAddrLine5 = consignmentData["consignor_party"]?["adress_line5"]?.ToString(),
                    ConsignorCountryId = consignmentData["consignor_party"]?["country_id"]?.ToString(),
                    
                    // Consignee mapping
                    ConsigneeName = consignmentData["consignee_party"]?["name"]?.ToString() ?? "",
                    ConsigneeAddrLine1 = consignmentData["consignee_party"]?["adress_line1"]?.ToString(),
                    ConsigneeAddrLine2 = consignmentData["consignee_party"]?["adress_line2"]?.ToString(),
                    ConsigneeAddrLine3 = consignmentData["consignee_party"]?["adress_line3"]?.ToString(),
                    ConsigneeAddrLine4 = consignmentData["consignee_party"]?["adress_line4"]?.ToString(),
                    ConsigneeAddrLine5 = consignmentData["consignee_party"]?["adress_line5"]?.ToString(),
                    ConsigneeCountryId = consignmentData["consignee_party"]?["country_id"]?.ToString(),
                    
                    // Country mapping
                    ExportCountryId = consignmentData["export_country_id"]?.ToString() ?? "",
                    ImportCountryId = consignmentData["import_country_id"]?.ToString() ?? "",
                    
                    // Unloading port
                    UnloadingBasePortId = (consignmentData["unloading_baseport"] as JObject)?["id"]?.ToString(),
                    UnloadingBasePortName = (consignmentData["unloading_baseport"] as JObject)?["name"]?.ToString(),
                    
                    ResponseStatus = "0101", 
                    TimeStamp = DateTime.Now,
                    LastUpdate = DateTime.Now,
                    MarkSendAsw = "N",
                    MarkSendAcfs = "N",
                    MarkSendIppc = "N",
                    QueueStatus = "IN-QUEUE"
                };

                _context.TabMessageThphytos.Add(thphytoEntity);

                // 4.1 Include Notes
                var includeNotes = docData["include_notes"] as JArray;
                if (includeNotes != null)
                {
                    foreach (var note in includeNotes)
                    {
                        var contents = note["contents"] as JArray;
                        var noteEntity = new TabMessageThphytoIncludedNote
                        {
                            MessageId = messageIdStr,
                            Subject = note["subject"]?.ToString() ?? "",
                            Content = contents != null 
                                ? string.Join(", ", contents.Select(c => c["content"]?.ToString()))
                                : "",
                            CreatedAt = DateTime.Now
                        };
                        _context.TabMessageThphytoIncludedNotes.Add(noteEntity);
                    }
                }

                // 4.2 Include Clauses
                var signatoryAuth = docData["signatory_authen"] as JObject;
                var includeClauses = signatoryAuth?["include_clauses"] as JArray;
                if (includeClauses != null)
                {
                    foreach (var clause in includeClauses)
                    {
                        var idStr = clause is JObject ? clause["id"]?.ToString() : clause.ToString();
                        if (int.TryParse(idStr, out int cId))
                        {
                            var clauseEntity = new TabMessageThphytoIncludedClause
                            {
                                MessageId = messageIdStr,
                                ClauseId = cId,
                                CreatedAt = DateTime.Now
                            };
                            _context.TabMessageThphytoIncludedClauses.Add(clauseEntity);
                        }
                    }
                }

                // 4.3 Transit Countries
                var transitCountries = consignmentData["transit_countries"] as JArray;
                if (transitCountries != null)
                {
                    foreach (var country in transitCountries)
                    {
                        var transitEntity = new TabMessageThphytoTransitCountry
                        {
                            MessageId = messageIdStr,
                            CountryId = country["id"]?.ToString() ?? "",
                            CountryName = country["name"]?.ToString(),
                            CreatedAt = DateTime.Now
                        };
                        _context.TabMessageThphytoTransitCountries.Add(transitEntity);
                    }
                }

                // 4.4 Main Carriages
                var mainCarriages = consignmentData["main_carriages"] as JArray;
                if (mainCarriages != null)
                {
                    foreach (var carriage in mainCarriages)
                    {
                        var carriageEntity = new TabMessageThphytoMainCarriage
                        {
                            MessageId = messageIdStr,
                            TransportModeCode = carriage["mode_code"]?.ToString(),
                            MovementId = carriage["id"]?.ToString(),
                            TransportMeanName = carriage["transport_mean_name"]?.ToString(),
                            CreatedAt = DateTime.Now
                        };
                        _context.TabMessageThphytoMainCarriages.Add(carriageEntity);
                    }
                }

                // 4.5 Items
                if (itemsData != null)
                {
                    foreach (var item in itemsData)
                    {
                        string itemIdStr = Guid.NewGuid().ToString();
                        
                        var netWeight = item["net_weight"] as JObject;
                        var grossWeight = item["gross_weight"] as JObject;
                        var netVolume = item["net_volume"] as JObject;
                        var grossVolume = item["gross_volume"] as JObject;

                        var itemEntity = new TabMessageThphytoItem
                        {
                            MessageId = messageIdStr,
                            ItemId = itemIdStr,
                            SequenceNo = item["sequence_no"]?.Value<int>() ?? 0,
                            ProductScientName = item["scient_name"]?.ToString(),
                            NetWeight = netWeight?["weight"]?.Value<decimal?>(),
                            NetWeightUnit = netWeight?["unit_code"]?.ToString(),
                            GrossWeight = grossWeight?["weight"]?.Value<decimal?>(),
                            GrossWeightUnit = grossWeight?["unit_code"]?.ToString(),
                            NetVolume = netVolume?["volume"]?.Value<double?>(),
                            NetVolumeUnit = netVolume?["unit_code"]?.ToString(),
                            GrossVolume = grossVolume?["volume"]?.Value<double?>(),
                            GrossVolumeUnit = grossVolume?["unit_code"]?.ToString(),
                            CreatedAt = DateTime.Now
                        };
                        
                        _context.TabMessageThphytoItems.Add(itemEntity);

                        // Item Descriptions
                        var descriptions = item["descriptions"] as JArray;
                        if (descriptions != null)
                        {
                            foreach (var desc in descriptions)
                            {
                                var descEntity = new TabMessageThphytoItemDescription
                                {
                                    MessageId = messageIdStr,
                                    ItemId = itemIdStr,
                                    ProductDescription = desc["name"]?.ToString(),
                                    CreatedAt = DateTime.Now
                                };
                                _context.TabMessageThphytoItemDescriptions.Add(descEntity);
                            }
                        }

                        // Item Common Names
                        var commonNames = item["common_names"] as JArray;
                        if (commonNames != null)
                        {
                            foreach (var name in commonNames)
                            {
                                var nameEntity = new TabMessageThphytoItemCommonName
                                {
                                    MessageId = messageIdStr,
                                    ItemId = itemIdStr,
                                    ProudctCommonName = name["name"]?.ToString(),
                                    CreatedAt = DateTime.Now
                                };
                                _context.TabMessageThphytoItemCommonNames.Add(nameEntity);
                            }
                        }

                        // Item Origin Countries
                        var originCountries = item["origin_countries"] as JArray;
                        if (originCountries != null)
                        {
                            foreach (var origin in originCountries)
                            {
                                var originEntity = new TabMessageThphytoItemOriginCountry
                                {
                                    MessageId = messageIdStr,
                                    ItemId = itemIdStr,
                                    CountryId = origin["id"]?.ToString() ?? "",
                                    CountryName = origin["name"]?.ToString(),
                                    SubDivisionId = origin["subdivision_id"]?.ToString(),
                                    SubDivisionName = origin["subdivision_name"]?.ToString(),
                                    CreatedAt = DateTime.Now
                                };
                                _context.TabMessageThphytoItemOriginCountries.Add(originEntity);
                            }
                        }

                        // Item Intended Uses
                        var intendUses = item["intend_uses"] as JArray;
                        if (intendUses != null)
                        {
                            foreach (var use in intendUses)
                            {
                                var useEntity = new TabMessageThphytoItemIntended
                                {
                                    MessageId = messageIdStr,
                                    ItemId = itemIdStr,
                                    ProductIntendUse = use["name"]?.ToString(),
                                    CreatedAt = DateTime.Now
                                };
                                _context.TabMessageThphytoItemIntendeds.Add(useEntity);
                            }
                        }

                        // Item Additional Notes
                        var additionalNotes = item["additional_notes"] as JArray;
                        if (additionalNotes != null)
                        {
                            foreach (var note in additionalNotes)
                            {
                                string noteIdStr = Guid.NewGuid().ToString();
                                var noteEntity = new TabMessageThphytoItemAdditionalNote
                                {
                                    MessageId = messageIdStr,
                                    ItemId = itemIdStr,
                                    AdditionalNoteId = noteIdStr,
                                    Subject = note["subject"]?.ToString() ?? "",
                                    CreatedAt = DateTime.Now
                                };
                                _context.TabMessageThphytoItemAdditionalNotes.Add(noteEntity);

                                var noteContents = note["contents"] as JArray;
                                if (noteContents != null)
                                {
                                    foreach (var content in noteContents)
                                    {
                                        var contentEntity = new TabMessageThphytoItemAdditionalNoteContent
                                        {
                                            MessageId = messageIdStr,
                                            ItemId = itemIdStr,
                                            AdditionalNoteId = noteIdStr,
                                            NoteContent = content["content"]?.ToString(),
                                            CreatedAt = DateTime.Now
                                        };
                                        _context.TabMessageThphytoItemAdditionalNoteContents.Add(contentEntity);
                                    }
                                }
                            }
                        }

                        // Item Applied Processes
                        var appliedProcesses = item["applied_processes"] as JArray;
                        if (appliedProcesses != null)
                        {
                            foreach (var process in appliedProcesses)
                            {
                                if (process["type_code"] == null || string.IsNullOrEmpty(process["type_code"]?.ToString()))
                                    continue;

                                string processIdStr = Guid.NewGuid().ToString();
                                var period = process["complete_period"] as JObject;
                                var processEntity = new TabMessageThphytoItemProcess
                                {
                                    MessageId = messageIdStr,
                                    ItemId = itemIdStr,
                                    ProcessId = processIdStr,
                                    TypeCode = process["type_code"]?.ToString() ?? "",
                                    StartDate = period?["start_date"]?.ToString(),
                                    EndDate = period?["end_date"]?.ToString(),
                                    Duration = period?["duration"]?.Value<double?>(),
                                    DurationUnit = period?["unit_code"]?.ToString(),
                                    CreatedAt = DateTime.Now
                                };
                                _context.TabMessageThphytoItemProcesses.Add(processEntity);

                                var characteristics = process["characteristics"] as JArray;
                                if (characteristics != null)
                                {
                                    foreach (var charac in characteristics)
                                    {
                                        var deec2 = charac["deec_2"] as JArray;
                                        var charEntity = new TabMessageThphytoItemProcessCharacteristic
                                        {
                                            MessageId = messageIdStr,
                                            ItemId = itemIdStr,
                                            ProcessId = processIdStr,
                                            TypeCode = process["type_code"]?.ToString(),
                                            Description1 = charac["desc_1"]?.ToString(),
                                            Description2 = deec2 != null 
                                                ? string.Join(", ", deec2.Select(d => d["value"]?.ToString()))
                                                : "",
                                            UnitCode = charac["unit_code"]?.ToString(),
                                            CreatedAt = DateTime.Now
                                        };
                                        _context.TabMessageThphytoItemProcessCharacteristics.Add(charEntity);
                                    }
                                }
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    status = "Success",
                    message = "E-Phyto document saved successfully",
                    data = new
                    {
                        messageId = messageIdStr,
                        docId = thphytoEntity.DocId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ASW-ePhytoNormal submission");
                return StatusCode(500, new
                {
                    status = "Error",
                    message = "An error occurred while processing the request",
                    details = ex.Message,
                    innerException = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "EPhyto Controller is working!", timestamp = DateTime.Now });
        }
    }
}
