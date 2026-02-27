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
        // Channel<T> = queue ที่ thread-safe ของ .NET
        private readonly Channel<EPhytoProgressRequest> _channel;

        public ProgressQueue()
        {
            // CreateUnbounded = queue ไม่จำกัดขนาด
            // ถ้าต้องการจำกัด เช่น ไม่เกิน 100 → ใช้ Channel.CreateBounded(100)
            _channel = Channel.CreateUnbounded<EPhytoProgressRequest>();
        }

        // Controller เรียก Enqueue() เพื่อส่งงานเข้า queue
        // TryWrite = ใส่ของลงท่อ → return ทันที ไม่รอ
        public void Enqueue(EPhytoProgressRequest request)
        {
            _channel.Writer.TryWrite(request);
        }

        // BackgroundService เรียก DequeueAsync() เพื่อรับงานออกจาก queue
        // ReadAsync = รอจนกว่าจะมีของใน queue แล้วค่อยดึงออกมา
        // ถ้า queue ว่าง → จะ await ไว้เฉยๆ ไม่กิน CPU
        public async Task<EPhytoProgressRequest> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _channel.Reader.ReadAsync(cancellationToken);
        }
    }
}
