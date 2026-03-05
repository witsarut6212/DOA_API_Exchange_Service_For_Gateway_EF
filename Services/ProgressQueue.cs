using System.Threading.Channels;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;

namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    public class ProgressQueue : IProgressQueue
    {
        private readonly Channel<(int payloadId, EPhytoProgressRequest request)> _channel;

        public ProgressQueue()
        {
            _channel = Channel.CreateUnbounded<(int payloadId, EPhytoProgressRequest request)>();
        }

        public void Enqueue(int payloadId, EPhytoProgressRequest request)
        {
            _channel.Writer.TryWrite((payloadId, request));
        }

        public async Task<(int payloadId, EPhytoProgressRequest request)> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _channel.Reader.ReadAsync(cancellationToken);
        }
    }
}
