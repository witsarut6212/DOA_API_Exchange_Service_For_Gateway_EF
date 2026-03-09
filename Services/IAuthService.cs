namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    public interface IAuthService
    {
        string GenerateJwtToken(string username);
        bool ValidateCredentials(string username, string password);
        string GenerateApplicationJwtToken(string appName, string appNickName, int tokenLifetimeMinutes);
        TokenRequestValidationResult ValidateTokenRequest(string? credentialValue);
    }

    public record TokenRequestValidationResult(bool IsValid, string Detail, List<(string Field, string Description)>? Validations);
}
