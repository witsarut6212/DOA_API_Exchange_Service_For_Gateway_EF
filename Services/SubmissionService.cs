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

        // =========================================================
        // STEP 1: เซฟลงตาราง response_payload ก่อนตอบ 200
        // =========================================================
        public async Task<bool> SaveResponsePayloadAsync(EPhytoProgressRequest request)
        {
            try
            {
                var payload = new TabMessageRepsonsePayload
                {
                    Status = "WAIT",
                    DataObject = Newtonsoft.Json.JsonConvert.SerializeObject(request),
                    CreatedAt = DateTime.Now,
                    CreatedBy = "SYSTEM"
                };

                _context.TabMessageRepsonsePayloads.Add(payload);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Step1 done - Created response_payload for Ref: {Ref}", 
                    request.DocumentControl.ReferenceNumber);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Step1 failed - Error saving payload for Ref: {Ref}", 
                    request.DocumentControl.ReferenceNumber);
                return false;
            }
        }

        // =========================================================
        // STEP 2 (Background): ประมวลผลเบื้องหลัง
        // =========================================================
        public async Task ProcessPayloadAsync(EPhytoProgressRequest request)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // ใช้ ReferenceNumber หรือ DocumentNumber ในการหา Message เดิมในระบบ
                    var referenceNo = request.DocumentControl.ReferenceNumber;
                    var newStatus = request.DocumentControl.ResponseInfo.Status;
                    var updateTime = request.DocumentControl.ResponseInfo.DateTime;

                    // แก้ไข: ค้นหาจากตาราง thphyto โดยอิงจาก DocId หรือ Reference 
                    // (ตรงนี้คุณอาจต้องเช็คว่าใน DB ของคุณใช้ column ไหนเก็บค่านี้)
                    var thphyto = await _context.TabMessageThphytos
                        .FirstOrDefaultAsync(x => x.DocId == referenceNo || x.MessageId == referenceNo);

                    if (thphyto != null)
                    {
                        thphyto.MessageStatus = newStatus;
                        thphyto.ResponseStatus = request.DocumentControl.ResponseInfo.Code; // เก็บ code ตอบกลับ
                        thphyto.ResponseAt = updateTime;
                        thphyto.LastUpdate = DateTime.Now;

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        _logger.LogInformation("Step2 done - Processed background for Ref: {Ref}", referenceNo);
                    }
                    else
                    {
                        _logger.LogWarning("Background Process - ReferenceNumber not found in thphyto: {Ref}", referenceNo);
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Step2 failed - Background Processing for Ref: {Ref}", 
                        request.DocumentControl.ReferenceNumber);
                }
            });
        }
    }
}
