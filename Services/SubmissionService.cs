using DOA_API_Exchange_Service_For_Gateway.Data;
using DOA_API_Exchange_Service_For_Gateway.Models.Entities;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;
using Microsoft.EntityFrameworkCore;

namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    public class SubmissionService : ISubmissionService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SubmissionService> _logger;

        public SubmissionService(AppDbContext context, ILogger<SubmissionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> UpdateProgressAsync(EPhytoProgressRequest request)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 1. ค้นหาข้อมูลหลักที่ต้องการอัปเดต (ตัวอย่าง: tab_message_thphyto)
                    var thphyto = await _context.TabMessageThphytos
                        .FirstOrDefaultAsync(x => x.MessageId == request.MessageId);

                    if (thphyto != null)
                    {
                        // อัปเดตสถานะในตารางหลัก
                        thphyto.MessageStatus = request.Status;
                        thphyto.LastUpdate = DateTime.Now;
                        // เพิ่ม logic อื่นๆ เช่นอัปเดต Remarks
                    }

                    // 2. บันทึกข้อมูลลงใน Table ใหม่ (ตัวอย่าง: tab_message_repsonse_payloads)
                    var responsePayload = new TabMessageRepsonsePayload
                    {
                        Status = request.Status,
                        DataObject = Newtonsoft.Json.JsonConvert.SerializeObject(request),
                        CreatedAt = DateTime.Now,
                        CreatedBy = "SYSTEM"
                    };
                    _context.TabMessageRepsonsePayloads.Add(responsePayload);

                    // 3. บันทึกเงื่อนไขอื่นๆ ตาม Business Logic
                    // เช่น ถ้า Status เป็น 'SUCCESS' ให้ไป Insert ในอีกตาราง
                    if (request.Status == "SUCCESS")
                    {
                        // Logic เพิ่มเติม...
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    _logger.LogInformation("Successfully updated progress for MessageId: {MessageId}", request.MessageId);
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error updating progress for MessageId: {MessageId}", request.MessageId);
                    return false;
                }
            });
        }
    }
}
