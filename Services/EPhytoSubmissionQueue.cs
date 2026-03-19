using System.Threading.Channels;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;

namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    public class EPhytoSubmissionQueue : IEPhytoSubmissionQueue
    {
        private readonly Channel<(int payloadId, object request, string source, string systemOrigin)> _queue;

        public EPhytoSubmissionQueue()
        {
            _queue = Channel.CreateUnbounded<(int, object, string, string)>();
        }

        public void Enqueue(int payloadId, object request, string source, string systemOrigin)
        {
            _queue.Writer.TryWrite((payloadId, request, source, systemOrigin));
        }

        public async Task<(int payloadId, object request, string source, string systemOrigin)> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}
