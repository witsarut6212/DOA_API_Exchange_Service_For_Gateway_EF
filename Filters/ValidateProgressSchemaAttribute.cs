using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using DOA_API_Exchange_Service_For_Gateway.Models;
using System.Text;

namespace DOA_API_Exchange_Service_For_Gateway.Filters
{
    // เปลี่ยนจาก ActionFilterAttribute เป็น IAsyncResourceFilter
    // เพื่อให้ทำงาน "ก่อน" Model Binding จะเริ่ม (ทำให้เราอ่าน Body ดิบๆ ได้)
    public class ValidateProgressSchemaAttribute : Attribute, IAsyncResourceFilter
    {
        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            var request = context.HttpContext.Request;
            var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var env = context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();

            // มาร์คหน้ากล่องเพื่อให้รู้ว่ายามคนนี้ได้ตรวจแล้วจริงๆ
            context.HttpContext.Response.Headers.Append("X-Schema-Validated", "True");
            
            // สำคัญมาก: ต้อง EnableBuffering เพื่อให้อ่าน Body ได้มากกว่า 1 ครั้ง
            request.EnableBuffering();
            
            string body = string.Empty;
            // อ่านค่าจาก Stream โดยตรง
            using (var reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024, true))
            {
                body = await reader.ReadToEndAsync();
                request.Body.Position = 0; // ต้อง Reset ทันทีหลังอ่านเสร็จ
            }

            // ส่งความยาว Body ไปให้พี่ดูใน Postman (Tab Headers)
            context.HttpContext.Response.Headers.Append("X-Debug-Body-Length", body.Length.ToString());

            if (string.IsNullOrEmpty(body))
            {
                var title = config["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";
                context.Result = new BadRequestObjectResult(new ApiResponse<object>
                {
                    Info = new ApiInfo { Title = title, Status = 400, Detail = "Request body is empty." },
                    Error = new ApiError { TraceId = context.HttpContext.TraceIdentifier, Instance = request.Path }
                });
                return;
            }

            try
            {
                var json = JObject.Parse(body);
                
                string code = json.SelectToken("documentControl.responseInfo.code")?.ToString() ?? "";
                string schemaFileName = "ProgressSchema_Standard.json"; 
                
                if (code == "AC009") schemaFileName = "ProgressSchema_Payment_AC009.json";
                else if (code == "AC015") schemaFileName = "ProgressSchema_Certificate_AC015.json";

                string schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "Schemas", schemaFileName);
                if (!File.Exists(schemaPath))
                {
                    schemaPath = Path.Combine(env.ContentRootPath, "Storage", "Schemas", schemaFileName);
                }

                if (File.Exists(schemaPath))
                {
                    var schema = JSchema.Parse(File.ReadAllText(schemaPath));

                    // ตรวจสอบความถูกต้อง (รวมถึง additionalProperties: false)
                    if (!json.IsValid(schema, out IList<string> errors))
                    {
                        var title = config["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";
                        
                        var response = new ApiResponse<object>
                        {
                            Info = new ApiInfo
                            {
                                Title = title,
                                Status = 422,
                                Detail = "One or more field validation failed. (Strict Schema Check)",
                                Timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssK")
                            },
                            Error = new ApiError
                            {
                                TraceId = context.HttpContext.TraceIdentifier,
                                Instance = request.Path
                            },
                            Validations = errors.Select(e => new ApiValidation
                            {
                                Field = "JSON Schema",
                                Description = e
                            }).ToList()
                        };

                        context.Result = new UnprocessableEntityObjectResult(response);
                        return; 
                    }
                }
                else
                {
                    throw new FileNotFoundException($"Schema file not found: {schemaFileName}");
                }
            }
            catch (Newtonsoft.Json.JsonReaderException ex)
            {
                var title = config["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";
                context.Result = new BadRequestObjectResult(new ApiResponse<object>
                {
                    Info = new ApiInfo { Title = title, Status = 400, Detail = $"Invalid JSON structure: {ex.Message}" },
                    Error = new ApiError { TraceId = context.HttpContext.TraceIdentifier, Instance = request.Path }
                });
                return;
            }
            catch (Exception ex)
            {
                var title = config["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";
                context.Result = new ObjectResult(new ApiResponse<object>
                {
                    Info = new ApiInfo { Title = title, Status = 500, Detail = $"Internal server error during validation: {ex.Message}" },
                    Error = new ApiError { TraceId = context.HttpContext.TraceIdentifier, Instance = request.Path }
                }) { StatusCode = 500 };
                return;
            }

            await next(); 
        }
    }
}
