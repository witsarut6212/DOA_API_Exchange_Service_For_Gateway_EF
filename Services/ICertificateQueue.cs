using DOA_API_Exchange_Service_For_Gateway.Models.Requests;

namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    public interface ICertificateQueue
    {
        void Enqueue(int payloadId, EPhytoCertificateRequest request, string source);
        Task<(int payloadId, EPhytoCertificateRequest request, string source)> DequeueAsync(CancellationToken cancellationToken);
    }
}
