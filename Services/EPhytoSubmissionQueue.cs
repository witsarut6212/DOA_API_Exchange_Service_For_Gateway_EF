using System.Threading.Channels;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;

namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    public class EPhytoSubmissionQueue : IEPhytoSubmissionQueue
    {
        private readonly Channel<(int payloadId, EPhytoRequest request, string source, string systemOrigin)> _channel;

        public EPhytoSubmissionQueue()
        {
            _channel = Channel.CreateUnbounded<(int payloadId, EPhytoRequest request, string source, string systemOrigin)>();
        }

        public void Enqueue(int payloadId, EPhytoRequest request, string source, string systemOrigin)
        {
            _channel.Writer.TryWrite((payloadId, request, source, systemOrigin));
        }

        public async Task<(int payloadId, EPhytoRequest request, string source, string systemOrigin)> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _channel.Reader.ReadAsync(cancellationToken);
        }
    }
}
