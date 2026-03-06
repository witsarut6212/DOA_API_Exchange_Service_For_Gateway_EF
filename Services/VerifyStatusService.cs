using DOA_API_Exchange_Service_For_Gateway.Models.Entities;

namespace DOA_API_Exchange_Service_For_Gateway.Services;

public interface IVerifyStatusService
{
    (int StatusCode, string Message) ValidateStatus(ApplicationExternal app);
}

public class VerifyStatusService : IVerifyStatusService
{
    public (int StatusCode, string Message) ValidateStatus(ApplicationExternal app)
    {
        // Check IsActive
        if (app.IsActive != "Y")
        {
            return (400, "ยังไม่พร้อมให้ใช้งาน");
        }

        // Check IsVerified
        if (app.IsVerified == "Y")
        {
            return (400, "ผ่านการตรวจสอบแล้ว");
        }

        return (200, "Status is valid");
    }
}
