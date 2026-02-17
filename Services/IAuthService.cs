namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    public interface IAuthService
    {
        string GenerateJwtToken(string username);
        bool ValidateCredentials(string username, string password);
    }
}
