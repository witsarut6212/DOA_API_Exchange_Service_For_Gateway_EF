using DOA_API_Exchange_Service_For_Gateway.Models.Requests;

namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    public interface ISubmissionService
    {
        Task<int> SaveResponsePayloadAsync(string rawDataObject, string source, string? docId = null);
        Task ProcessPayloadAsync(int payloadId, EPhytoProgressRequest request, string source);
        Task<bool> IsDocumentNumberDuplicateAsync(string? documentNumber);
        Task<int> SaveCertificatePayloadAsync(string rawDataObject, string source, EPhytoCertificateRequest request);
        Task ProcessCertificatePayloadAsync(int payloadId, EPhytoCertificateRequest request, string source);
        Task<bool> CanEditCertificateAsync(string referenceNumber);
    }
}
