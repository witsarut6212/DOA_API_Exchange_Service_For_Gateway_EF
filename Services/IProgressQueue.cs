using DOA_API_Exchange_Service_For_Gateway.Models.Requests;

namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    // Interface ของ Queue ที่ใช้ส่งงานไป Background Service
    public interface IProgressQueue
    {
        // ใส่ request เข้า queue → เรียกจาก Controller
        void Enqueue(EPhytoProgressRequest request);

        // ดึง request ออกจาก queue → เรียกจาก BackgroundService
        // จะรอ (block) จนกว่า queue จะมีของ หรือถูก cancel
        Task<EPhytoProgressRequest> DequeueAsync(CancellationToken cancellationToken);
    }
}
