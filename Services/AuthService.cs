using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;

        public AuthService(IConfiguration configuration)
        {
            _configuration = configuration;
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
                    new Claim("app_name", appName ?? string.Empty),
                    new Claim("app_nick_name", appNickName ?? string.Empty)
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

        public TokenRequestValidationResult ValidateTokenRequest(string? credentialValue)
        {
            if (string.IsNullOrWhiteSpace(credentialValue))
            {
                return new TokenRequestValidationResult(
                    false,
                    "One or more field validation failed.",
                    new List<(string Field, string Description)>
                    {
                        ("credential_value", "The credential_value field is required.")
                    });
            }
            return new TokenRequestValidationResult(true, string.Empty, null);
        }
    }
}
