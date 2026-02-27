using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using DOA_API_Exchange_Service_For_Gateway.Models;
using System.Text;

namespace DOA_API_Exchange_Service_For_Gateway.Filters
{
    // ยามเฝ้าประตู: ตรวจสอบ JSON Schema ตาม Code แบบอัจฉริยะ (Dynamic Validation)
    public class ValidateProgressSchemaAttribute : ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var request = context.HttpContext.Request;
            var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var env = context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
            
            // อ่านค่า JSON จาก Body
            request.EnableBuffering();
            string body;
            using (var reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024, true))
            {
                body = await reader.ReadToEndAsync();
                request.Body.Position = 0; // รีเซ็ตเพื่อนำไปใช้งานต่อ
            }

            if (!string.IsNullOrEmpty(body))
            {
                try
                {
                    var json = JObject.Parse(body);
                    
                    // 1. ดึงค่า Code จาก JSON เพื่อตัดสินใจเลือกคู่มือ (Schema)
                    string code = json.SelectToken("documentControl.responseInfo.code")?.ToString() ?? "";
                    
                    // ตั้งค่าเริ่มต้นเป็นไฟล์ Standard (หากไม่มี code ใน JSON ค่อยให้ Standard ตรวจสอบอีกทีว่าขาด)
                    string schemaFileName = "ProgressSchema_Standard.json"; 
                    
                    if (code == "AC009")
                    {
                        schemaFileName = "ProgressSchema_Payment_AC009.json";
                    }
                    else if (code == "AC015")
                    {
                        schemaFileName = "ProgressSchema_Certificate_AC015.json";
                    }

                    // 2. เรียกใช้งาน Schema จากไฟล์
                    string schemaPath = Path.Combine(env.ContentRootPath, "Storage", "Schemas", schemaFileName);

                    if (File.Exists(schemaPath))
                    {
                        var schema = JSchema.Parse(File.ReadAllText(schemaPath));

                        // 3. ตรวจสอบความถูกต้อง! ไม่ผ่านเตรียมปา 422
                        if (!json.IsValid(schema, out IList<string> errors))
                        {
                            var title = config["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";
                            
                            var response = new ApiResponse<object>
                            {
                                Info = new ApiInfo
                                {
                                    Title = title,
                                    Status = 422,
                                    Detail = "One or more field validation failed.",
                                    Timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssK")
                                },
                                Error = new ApiError
                                {
                                    TraceId = context.HttpContext.TraceIdentifier,
                                    Instance = request.Path
                                },
                                // เอาข้อความ Error จากการตรวจมาใส่ใน Validations
                                Validations = errors.Select(e => new ApiValidation
                                {
                                    Field = "JSON Schema",
                                    Description = e
                                }).ToList()
                            };

                            context.Result = new UnprocessableEntityObjectResult(response);
                            return; // ตีกลับทันที โค้ดใน Controller จะไม่มีวันได้ทำงาน
                        }
                    }
                }
                catch (Newtonsoft.Json.JsonReaderException)
                {
                    // รูปแบบปีกกาโครงสร้าง JSON พัง (เดี๋ยวชั้น Model Binding จะดัก 400 เป็นด่านต่อไป)
                }
                catch (Exception)
                {
                    // ข้อผิดพลาดอื่นๆ ปล่อยผ่านให้ API วิ่งต่อ
                }
            }

            await next(); // ผ่านไปทำงานใน Controller ได้
        }
    }
}
