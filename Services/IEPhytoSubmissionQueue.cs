using DOA_API_Exchange_Service_For_Gateway.Models.Requests;

namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    public interface IEPhytoSubmissionQueue
    {
        void Enqueue(int payloadId, EPhytoRequest request, string source, string systemOrigin);
        Task<(int payloadId, EPhytoRequest request, string source, string systemOrigin)> DequeueAsync(CancellationToken cancellationToken);
    }
}
