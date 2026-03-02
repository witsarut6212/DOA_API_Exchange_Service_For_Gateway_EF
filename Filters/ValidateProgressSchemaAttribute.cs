using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using DOA_API_Exchange_Service_For_Gateway.Models;
using System.Text;

namespace DOA_API_Exchange_Service_For_Gateway.Filters
{
    public class ValidateProgressSchemaAttribute : Attribute, IAsyncResourceFilter
    {
        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            var request = context.HttpContext.Request;
            var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var env = context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();

            context.HttpContext.Response.Headers.Append("X-Schema-Validated", "True");
            
            request.EnableBuffering();
            
            string body = string.Empty;
            using (var reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024, true))
            {
                body = await reader.ReadToEndAsync();
                request.Body.Position = 0; 
            }

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

                    if (!json.IsValid(schema, out IList<ValidationError> errors))
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
                            Validations = errors.Select(e => {
                                string description = e.Message;
                                
                                if (e.ErrorType == ErrorType.AdditionalProperties)
                                {
                                    var segments = e.Path.Split('.');
                                    string fieldName = segments.LastOrDefault() ?? "Unknown";
                                    int bracketIdx = fieldName.IndexOf('[');
                                    if (bracketIdx > 0) fieldName = fieldName.Substring(0, bracketIdx);

                                    description = $"Field {fieldName} is not required.";
                                }
                                else
                                {
                                    int pathIdx = description.IndexOf(". Path '");
                                    if (pathIdx > 0) description = description.Substring(0, pathIdx) + ".";
                                }

                                return new ApiValidation
                                {
                                    Field = e.Path,
                                    Description = description
                                };
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
