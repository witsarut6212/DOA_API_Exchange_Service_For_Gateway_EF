using System.Threading.Tasks;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;

namespace DOA_API_Exchange_Service_For_Gateway.Services;

public interface IApplicationService
{
    Task<(bool Success, string Message, object? Data)> RegisterApplicationAsync(ApplicationRegisterRequest request);
}
