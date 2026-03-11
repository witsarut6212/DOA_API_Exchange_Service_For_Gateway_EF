using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;

namespace DOA_API_Exchange_Service_For_Gateway.Helpers
{
    public class SwaggerHeaderFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                operation.Parameters = new List<OpenApiParameter>();

            // เพิ่ม client_id ให้กับ Endpoint /auth/token
            if (context.ApiDescription.RelativePath != null && context.ApiDescription.RelativePath.EndsWith("auth/token"))
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "client_id",
                    In = ParameterLocation.Header,
                    Description = "Application Client ID (UUID)",
                    Required = true,
                    Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "uuid"
                    }
                });
            }
        }
    }
}
