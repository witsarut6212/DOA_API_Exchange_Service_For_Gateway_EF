using DOA_API_Exchange_Service_For_Gateway.Models.Requests;

namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    public interface ISubmissionService
    {
        Task<int> SaveResponsePayloadAsync(string rawDataObject, string? docId = null);
        Task ProcessPayloadAsync(int payloadId, EPhytoProgressRequest request);
        Task<bool> IsDocumentNumberDuplicateAsync(string documentNumber);
    }
}
