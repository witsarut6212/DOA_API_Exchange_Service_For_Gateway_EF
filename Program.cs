using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using DOA_API_Exchange_Service_For_Gateway.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
    }); 

var connectionString = builder.Configuration.GetConnectionString("MySQL");
builder.Services.AddDbContext<DOA_API_Exchange_Service_For_Gateway.Data.AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 1,  
            maxRetryDelay: TimeSpan.FromSeconds(1),  
            errorNumbersToAdd: null
        )));

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "default_secret_key_at_least_32_chars_long";
var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{

    options.RequireHttpsMetadata = false; 

    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "DOA API Exchange Service For Gateway",
        Version = "v1",
        Description = "API สำหรับ Exchange Service ของ DOA Gateway",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "DOA Team"
        }
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {

        if (ex.InnerException is MySqlConnector.MySqlException 
            || ex is Microsoft.EntityFrameworkCore.Storage.RetryLimitExceededException
            || ex.Message.Contains("connect") 
            || ex.Message.ToLower().Contains("access denied") 
            || ex.Message.ToLower().Contains("transient"))
        {
            var config = context.RequestServices.GetRequiredService<IConfiguration>();
            var title = config["ReponseTitle:Title"] ?? "API Exchange Service For Gateway";

            var response = new ApiResponse<object>
            {
                Info = new ApiInfo
                {
                    Title = title,
                    Detail = "Cannot connect to database",
                    SystemCode = 503
                },
                Error = new ApiError
                {
                    TraceId = context.TraceIdentifier,
                    Instance = context.Request.Path
                }
            };

            context.Response.StatusCode = 503;
            context.Response.ContentType = "application/json";

            var jsonSettings = new JsonSerializerSettings 
            { 
                ContractResolver = new CamelCasePropertyNamesContractResolver() 
            };
            await context.Response.WriteAsync(JsonConvert.SerializeObject(response, jsonSettings));
        }
        else
        {
            var config = context.RequestServices.GetRequiredService<IConfiguration>();
            var title = config["ReponseTitle:Title"] ?? "API Exchange Service For Gateway";

            var response = new ApiResponse<object>
            {
                Info = new ApiInfo
                {
                    Title = title,
                    Detail = "The application process unsuccessful.",
                    SystemCode = 580
                },
                Error = new ApiError
                {
                    TraceId = context.TraceIdentifier,
                    Instance = context.Request.Path
                }
            };

            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            var jsonSettings = new JsonSerializerSettings 
            { 
                ContractResolver = new CamelCasePropertyNamesContractResolver() 
            };
            await context.Response.WriteAsync(JsonConvert.SerializeObject(response, jsonSettings));
        }
    }
});

app.Use(async (context, next) =>
{
    await next();
    if (context.Response.StatusCode == 404 && !context.Response.HasStarted)
    {
        var config = context.RequestServices.GetRequiredService<IConfiguration>();
        var title = config["ReponseTitle:Title"] ?? "API Exchange Service For Gateway";

        string detail = "the resource is not exists.";
        
        if (context.Request.Method == "GET")
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
                SystemCode = 404
            },
            Error = new ApiError
            {
                TraceId = context.TraceIdentifier,
                Instance = context.Request.Path
            }
        };

        context.Response.ContentType = "application/json";
        var jsonSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
        await context.Response.WriteAsync(JsonConvert.SerializeObject(response, jsonSettings));
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "DOA API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
