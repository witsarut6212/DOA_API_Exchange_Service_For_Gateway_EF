using DOA_API_Exchange_Service_For_Gateway.Models.Requests;

namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    public interface ISubmissionService
    {
        // Step 1: ตาม Flowchart -> ให้อินเสิร์ทลง response_payload (Status = WAIT) ก่อนตอบ 200
        Task<bool> SaveResponsePayloadAsync(EPhytoProgressRequest request);

        // Step 2: ทำงานเบื้องหลัง (process payload)
        Task ProcessPayloadAsync(EPhytoProgressRequest request);
    }
}
