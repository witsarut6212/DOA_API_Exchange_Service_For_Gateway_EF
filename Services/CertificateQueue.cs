using System.Threading.Channels;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;

namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    public class CertificateQueue : ICertificateQueue
    {
        private readonly Channel<(int payloadId, EPhytoCertificateRequest request, string source)> _channel;

        public CertificateQueue()
        {
            _channel = Channel.CreateUnbounded<(int payloadId, EPhytoCertificateRequest request, string source)>();
        }

        public void Enqueue(int payloadId, EPhytoCertificateRequest request, string source)
        {
            _channel.Writer.TryWrite((payloadId, request, source));
        }

        public async Task<(int payloadId, EPhytoCertificateRequest request, string source)> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _channel.Reader.ReadAsync(cancellationToken);
        }
    }
}
