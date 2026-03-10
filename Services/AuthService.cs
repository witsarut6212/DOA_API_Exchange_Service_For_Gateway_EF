using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DOA_API_Exchange_Service_For_Gateway.Data;
using DOA_API_Exchange_Service_For_Gateway.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using DOA_API_Exchange_Service_For_Gateway.Models.Entities;

namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _dbContext;
        private readonly IMemoryCache _cache;
        private const string AppCachePrefix = "App_";

        public AuthService(IConfiguration configuration, AppDbContext dbContext, IMemoryCache cache)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            _cache = cache;
        }

        private (byte[] Key, string? Issuer, string? Audience) GetJwtConfiguration()
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured.");
            var key = Encoding.ASCII.GetBytes(secretKey);
            return (key, jwtSettings["Issuer"], jwtSettings["Audience"]);
        }

        public string GenerateJwtToken(string username)
        {
            var (key, issuer, audience) = GetJwtConfiguration();

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, "Admin")
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public string GenerateApplicationJwtToken(string appName, string appNickName, int tokenLifetimeMinutes)
        {
            var (key, issuer, audience) = GetJwtConfiguration();

            if (tokenLifetimeMinutes <= 0)
            {
                tokenLifetimeMinutes = 15;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("AppName", appName ?? string.Empty),
                    new Claim("AppNickName", appNickName ?? string.Empty)
                }),
                Expires = DateTime.UtcNow.AddMinutes(tokenLifetimeMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public bool ValidateCredentials(string username, string password)
        {
            var authSettings = _configuration.GetSection("AuthCredentials");
            var validUser = authSettings["Username"];
            var validPass = authSettings["Password"];

            return validUser != null && validPass != null && username == validUser && password == validPass;
        }

        public TokenRequestValidationResult ValidateTokenRequest(string? credentialType)
        {
            var validations = new List<(string Field, string Description)>();

            if (string.IsNullOrWhiteSpace(credentialType))
            {
                validations.Add(("credential_type", "The credential_type field is required."));
            }
            else if (credentialType != "1")
            {
                validations.Add(("credential_type", "The credential_type must be '1'."));
            }

            if (validations.Any())
            {
                return new TokenRequestValidationResult(false, "One or more field validation failed.", validations);
            }

            return new TokenRequestValidationResult(true, string.Empty, null);
        }

        public async Task<IssueTokenResult> IssueTokenAsync(string? clientId, string? credentialType)
        {
            // 1. Validate client_id format (UUID)
            if (string.IsNullOrWhiteSpace(clientId) || !Guid.TryParse(clientId, out _))
            {
                return new IssueTokenResult(false, "ไม่ได้ลงทะเบียน.", 401);
            }

            // 2. Validate credential_type in body
            var validationResult = ValidateTokenRequest(credentialType);
            if (!validationResult.IsValid)
            {
                var validations = validationResult.Validations?
                    .Select(v => new ApiValidation { Field = v.Field, Description = v.Description })
                    .ToList();
                return new IssueTokenResult(false, validationResult.Detail, 422, null, validations);
            }

            // 3. Find application by client_id (Using Cache)
            var cacheKey = $"{AppCachePrefix}{clientId}";
            
            if (!_cache.TryGetValue(cacheKey, out ApplicationExternal? application))
            {
                // Cache Miss: Query from Database
                application = await _dbContext.ApplicationExternals
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.CliendId == clientId);

                if (application != null)
                {
                    // Cache Set (expire in 15 mins)
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromMinutes(15))
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

                    _cache.Set(cacheKey, application, cacheOptions);
                }
            }

            if (application == null)
            {
                return new IssueTokenResult(false, "ไม่ได้ลงทะเบียน.", 401);
            }

            // 4. Check IsActive flag
            if (!string.Equals(application.IsActive, "Y", StringComparison.OrdinalIgnoreCase))
            {
                return new IssueTokenResult(false, "ไม่ได้ลงทะเบียน.", 401);
            }

            // 5. Check IsVerified flag
            if (!string.Equals(application.IsVerified, "Y", StringComparison.OrdinalIgnoreCase))
            {
                return new IssueTokenResult(false, "ยังไม่พร้อมให้ใช้งาน.", 401);
            }

            // 6. Read token lifetime from configuration
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var lifetimeConfig = jwtSettings["TokenLifetimeMinutes"];
            var tokenLifetimeMinutes = 15;

            if (int.TryParse(lifetimeConfig, out var configuredLifetime) && configuredLifetime > 0)
            {
                tokenLifetimeMinutes = configuredLifetime;
            }

            // 7. Generate JWT token
            var token = GenerateApplicationJwtToken(application.AppName, application.AppNickName, tokenLifetimeMinutes);
            var expiredAt = DateTime.UtcNow.AddMinutes(tokenLifetimeMinutes);

            var data = new
            {
                token,
                expired_at = expiredAt.ToString("O")
            };

            return new IssueTokenResult(true, "ระบบตรวจสอบตัวตนเรียบร้อยแล้ว.", 200, data);
        }
    }
}
