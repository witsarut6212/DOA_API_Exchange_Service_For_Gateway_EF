using DOA_API_Exchange_Service_For_Gateway.Models.Requests;

namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    public interface ISubmissionService
    {
        Task<bool> UpdateProgressAsync(EPhytoProgressRequest request);
    }
}
