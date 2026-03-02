using System.Threading.Channels;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;

namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    // ProgressQueue ใช้ Channel<T> ของ .NET เป็น in-memory queue
    //
    // Channel<T> คืออะไร?
    // - เหมือนท่อ (pipe) ที่มีคนใส่ของ (Writer) และคนเอาออก (Reader)
    // - thread-safe ใช้ได้หลาย thread พร้อมกันอย่างปลอดภัย
    // - ถ้า queue ว่าง → Reader จะรอ (await) อัตโนมัติจนกว่าจะมีของ
    //
    // จดทะเบียนเป็น Singleton → มีตัวเดียวตลอดชีวิต app
    // ทำให้ Controller และ BackgroundService ใช้ queue ตัวเดียวกัน
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
