using System.Threading.Channels;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;

namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    /// <summary>
    /// In-memory queue สำหรับ EPhyto Submission (asw/normal, ippc/normal, ippc/re-export, ippc/withdraw)
    /// ใช้ Channel<T> ของ .NET — thread-safe, Singleton lifetime
    /// </summary>
    public class EPhytoSubmissionQueue : IEPhytoSubmissionQueue
    {
        private readonly Channel<(int payloadId, EPhytoRequest request, string source)> _channel;

        public EPhytoSubmissionQueue()
        {
            _channel = Channel.CreateUnbounded<(int payloadId, EPhytoRequest request, string source)>();
        }

        public void Enqueue(int payloadId, EPhytoRequest request, string source)
        {
            _channel.Writer.TryWrite((payloadId, request, source));
        }

        public async Task<(int payloadId, EPhytoRequest request, string source)> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _channel.Reader.ReadAsync(cancellationToken);
        }
    }
}
