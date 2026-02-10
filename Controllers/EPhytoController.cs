using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
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

        public EPhytoController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitEPhyto([FromBody] JObject jsonData)
        {
            try
            {
                // 1. JSON Schema Validation
                string schemaPath = Path.Combine(_env.ContentRootPath, "Schemas", "ephyto_schema.json");
                string schemaJson = await System.IO.File.ReadAllTextAsync(schemaPath);
                JSchema schema = JSchema.Parse(schemaJson);

                IList<string> errorMessages;
                bool isValid = jsonData.IsValid(schema, out errorMessages);

                if (!isValid)
                {
                    return BadRequest(new { 
                        message = "JSON Validation Failed", 
                        errors = errorMessages 
                    });
                }

                // 2. Map to Entities and Save (ตัวอย่างการดึงข้อมูลบางส่วนมาเซฟ)
                var docData = jsonData["xc_document"];
                
                var document = new EPhytoDocument
                {
                    DocId = docData["doc_id"]?.ToString() ?? "",
                    DocName = docData["doc_name"]?.ToString() ?? "",
                    DocType = docData["doc_type"]?.ToString() ?? "",
                    StatusCode = docData["status_code"]?.ToString() ?? "",
                    IssueDate = docData["issue_date"]?.Values<DateTime>().FirstOrDefault() ?? DateTime.Now,
                    IssuePartyName = docData["issue_party_name"]?.ToString() ?? ""
                };

                // เพิ่ม Note แรกเป็นตัวอย่าง
                var firstNote = docData["include_notes"]?[0];
                if (firstNote != null)
                {
                    document.Notes.Add(new DocumentNote {
                        Subject = firstNote["subject"]?.ToString() ?? "",
                        Content = firstNote["contents"]?[0]?["content"]?.ToString() ?? ""
                    });
                }

                _context.EPhytoDocuments.Add(document);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Validation Success and Data Saved", 
                    documentId = document.Id 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error", details = ex.Message });
            }
        }
    }
}
