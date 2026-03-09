using Microsoft.AspNetCore.Mvc;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;
using DOA_API_Exchange_Service_For_Gateway.Services;
using DOA_API_Exchange_Service_For_Gateway.Helpers;
using DOA_API_Exchange_Service_For_Gateway.Data;
using Microsoft.EntityFrameworkCore;

namespace DOA_API_Exchange_Service_For_Gateway.Controllers
{
    [Route("auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _dbContext;

        public AuthController(IAuthService authService, IConfiguration configuration, AppDbContext dbContext)
        {
            _authService = authService;
            _configuration = configuration;
            _dbContext = dbContext;
        }

        [HttpPost("login-mockup")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var title = _configuration["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";

            if (_authService.ValidateCredentials(request.Username, request.Password))
            {
                var token = _authService.GenerateJwtToken(request.Username);
                var data = new Dictionary<string, string> { { "token", token } };
                return Ok(ResponseWriter.CreateSuccess(title, data, "Authentication was susccessful."));
            }

            var errorResponse = ResponseWriter.CreateError(title, "Incorrect credentials: Entering the wrong username or password.", 401, HttpContext.TraceIdentifier, HttpContext.Request.Path);
            return Unauthorized(errorResponse);
        }

        [HttpPost("token")]
        public async Task<IActionResult> GenerateToken([FromBody] AuthTokenRequest request)
        {
            var title = _configuration["ResponseTitle:Title"] ?? "API Exchange Service For Gateway";

            // 1. Read client_id from Header (accept both client_id and cliend_id just in case)
            if (!Request.Headers.TryGetValue("client_id", out var clientIdValues) &&
                !Request.Headers.TryGetValue("cliend_id", out clientIdValues))
            {
                var errorMissingHeader = ResponseWriter.CreateError(
                    title,
                    "client_id header is required.",
                    401,
                    HttpContext.TraceIdentifier,
                    HttpContext.Request.Path);

                return BadRequest(errorMissingHeader);
            }

            var clientId = clientIdValues.ToString();

            // 2. Validate client_id format (UUID)
            if (string.IsNullOrWhiteSpace(clientId) || !Guid.TryParse(clientId, out _))
            {
                var errorInvalidClientId = ResponseWriter.CreateError(
                    title,
                    "ไม่ได้ลงทะเบียน.",
                    401,
                    HttpContext.TraceIdentifier,
                    HttpContext.Request.Path);

                return BadRequest(errorInvalidClientId);
            }

            // 3. Validate credential_value in body (message from service)
            var validationResult = _authService.ValidateTokenRequest(request?.CredentialValue);
            if (!validationResult.IsValid)
            {
                var validations = validationResult.Validations?
                    .Select(v => new Models.ApiValidation { Field = v.Field, Description = v.Description })
                    .ToList();
                var errorInvalidCredential = ResponseWriter.CreateError(
                    title,
                    validationResult.Detail,
                    422,
                    HttpContext.TraceIdentifier,
                    HttpContext.Request.Path,
                    null,
                    validations);

                return UnprocessableEntity(errorInvalidCredential);
            }

            // 4. Find application by client_id
            var application = await _dbContext.ApplicationExternals
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.CliendId == clientId);

            if (application == null)
            {
                var errorNotRegistered = ResponseWriter.CreateError(
                    title,
                    "ไม่ได้ลงทะเบียน",
                    401,
                    HttpContext.TraceIdentifier,
                    HttpContext.Request.Path);

                return BadRequest(errorNotRegistered);
            }

            // 5. (Optional) Validate credential_value against stored SecretKey – ปิดไว้ตามที่ต้องการไม่เช็ค
            // if (string.IsNullOrEmpty(application.SecretKey) ||
            //     !string.Equals(application.SecretKey, request.CredentialValue, StringComparison.Ordinal))
            // {
            //     var errorInvalidSecret = ResponseWriter.CreateError(
            //         title,
            //         "Invalid credential_value.",
            //         400,
            //         HttpContext.TraceIdentifier,
            //         HttpContext.Request.Path);
            //     return BadRequest(errorInvalidSecret);
            // }

            // 6. Check IsActive flag
            if (!string.Equals(application.IsActive, "Y", StringComparison.OrdinalIgnoreCase))
            {
                var errorInactive = ResponseWriter.CreateError(
                    title,
                    "ไม่ได้ลงทะเบียน",
                    401,
                    HttpContext.TraceIdentifier,
                    HttpContext.Request.Path);

                return BadRequest(errorInactive);
            }

            // 7. Check IsVerified flag
            if (!string.Equals(application.IsVerified, "Y", StringComparison.OrdinalIgnoreCase))
            {
                var errorNotVerified = ResponseWriter.CreateError(
                    title,
                    "ยังไม่พร้อมให้ใช้งาน",
                    401,
                    HttpContext.TraceIdentifier,
                    HttpContext.Request.Path);

                return BadRequest(errorNotVerified);
            }

            // 8. Read token lifetime from configuration (minutes)
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var lifetimeConfig = jwtSettings["TokenLifetimeMinutes"];
            var tokenLifetimeMinutes = 15;

            if (int.TryParse(lifetimeConfig, out var configuredLifetime) && configuredLifetime > 0)
            {
                tokenLifetimeMinutes = configuredLifetime;
            }

            // 9. Generate JWT token with AppName and AppNickName in claims
            var token = _authService.GenerateApplicationJwtToken(application.AppName, application.AppNickName, tokenLifetimeMinutes);
            var expiredAt = DateTime.UtcNow.AddMinutes(tokenLifetimeMinutes);

            var data = new
            {
                token,
                expired_at = expiredAt.ToString("O")
                //app_name = application.AppNickName
            };

            return Ok(ResponseWriter.CreateSuccess(title, data, "Token generated successfully."));
        }
    }
}
