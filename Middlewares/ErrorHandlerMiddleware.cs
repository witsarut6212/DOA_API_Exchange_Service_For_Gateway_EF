using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DOA_API_Exchange_Service_For_Gateway.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DOA_API_Exchange_Service_For_Gateway.Middlewares
{
    public class ErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlerMiddleware> _logger;
        private readonly IConfiguration _configuration;

        public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);

                if ((context.Response.StatusCode == (int)HttpStatusCode.NotFound || 
                     context.Response.StatusCode == (int)HttpStatusCode.Unauthorized) && 
                    !context.Response.HasStarted)
                {
                    await HandleCustomStatusCodes(context);
                }
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(exception, "An unhandled exception occurred.");

            var title = _configuration["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";
            var response = new ApiResponse<object>
            {
                Info = new ApiInfo
                {
                    Title = title
                },
                Error = new ApiError
                {
                    TraceId = context.TraceIdentifier,
                    Instance = context.Request.Path
                }
            };

            var statusCode = HttpStatusCode.InternalServerError;

            if (IsDatabaseConnectionError(exception))
            {
                statusCode = HttpStatusCode.ServiceUnavailable;
                response.Info.Detail = "Cannot connect to database";
                response.Info.SystemCode = 503;
            }
            else
            {
                response.Info.Detail = "The application process unsuccessful.";
                response.Info.SystemCode = 580;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var jsonSettings = new JsonSerializerSettings 
            { 
                ContractResolver = new CamelCasePropertyNamesContractResolver() 
            };
            await context.Response.WriteAsync(JsonConvert.SerializeObject(response, jsonSettings));
        }

        private async Task HandleCustomStatusCodes(HttpContext context)
        {
            var title = _configuration["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";
            
            string detail = context.Response.StatusCode == 404 
                ? "the resource is not exists." 
                : "Authentication failed.";

            if (context.Response.StatusCode == 401 && 
                context.Request.Path.Value?.Contains("login-mockup", StringComparison.OrdinalIgnoreCase) == true)
            {
                detail = "Incorrect credentials: Entering the wrong username or password.";
            }

            if (context.Response.StatusCode == 404 && context.Request.Method == "GET")
            {
                string? docId = GetDocIdFromRequest(context);
                if (!string.IsNullOrEmpty(docId))
                {
                    detail = $"Document {docId} was not found.";
                }
            }

            var response = new ApiResponse<object>
            {
                Info = new ApiInfo
                {
                    Title = title,
                    Detail = detail,
                    SystemCode = context.Response.StatusCode
                }
            };

            context.Response.ContentType = "application/json";
            var jsonSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            await context.Response.WriteAsync(JsonConvert.SerializeObject(response, jsonSettings));
        }

        private bool IsDatabaseConnectionError(Exception ex)
        {
            return ex.InnerException is MySqlConnector.MySqlException 
                || ex is Microsoft.EntityFrameworkCore.Storage.RetryLimitExceededException
                || ex.Message.ToLower().Contains("connect") 
                || ex.Message.ToLower().Contains("access denied") 
                || ex.Message.ToLower().Contains("transient");
        }

        private string? GetDocIdFromRequest(HttpContext context)
        {
            string? docId = context.Request.Query["doc_id"];
            if (string.IsNullOrEmpty(docId))
            {
                var pathParts = context.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (pathParts != null && pathParts.Length > 0)
                {
                    var lastPart = pathParts.Last();
                    if (!lastPart.Equals("ephyto", StringComparison.OrdinalIgnoreCase) && 
                        !lastPart.Equals("auth", StringComparison.OrdinalIgnoreCase))
                    {
                        docId = lastPart;
                    }
                }
            }
            return docId;
        }
    }

    public static class ErrorHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseErrorHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ErrorHandlerMiddleware>();
        }
    }
}
