using DOA_API_Exchange_Service_For_Gateway.Models.Requests;

namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    public interface IProgressQueue
    {
        void Enqueue(int payloadId, EPhytoProgressRequest request, string source);
        Task<(int payloadId, EPhytoProgressRequest request, string source)> DequeueAsync(CancellationToken cancellationToken);
    }
}
