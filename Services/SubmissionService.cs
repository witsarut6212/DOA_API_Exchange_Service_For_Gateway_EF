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
        private readonly ILogService _logService;
        private readonly IConfiguration _configuration;
        private readonly string _logInstancePath;

        public SubmissionService(AppDbContext context, ILogger<SubmissionService> logger, ILogService logService, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _logService = logService;
            _configuration = configuration;
            _logInstancePath = _configuration["ApiSettings:SubmissionProgressPath"] ?? "UNKNOWN_PATH";
        }

        public async Task<int> SaveResponsePayloadAsync(EPhytoProgressRequest request)
        {
            try
            {
                var payload = new TabMessageResponsePayload
                {
                    Status = "WAIT",
                    DataObject = Newtonsoft.Json.JsonConvert.SerializeObject(request),
                    CreatedAt = DateTime.Now,
                    CreatedBy = "SYSTEM"
                };

                _context.TabMessageResponsePayloads.Add(payload);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Step 1: Saved payload for Ref: {Ref} (ID: {Id})", 
                    request.DocumentControl.ReferenceNumber, payload.Id);
                
                return payload.Id;
            }
            catch (Exception ex)
            {
                await _logService.LogExceptionAsync(ex, _logInstancePath);
                _logger.LogError(ex, "Step 1 Failed: Error saving payload for Ref: {Ref}", 
                    request.DocumentControl.ReferenceNumber);
                return 0;
            }
        }

        public async Task ProcessPayloadAsync(int payloadId, EPhytoProgressRequest request)
        {
            // 1. Update Record response_payload (Status = PROCESSING)
            var payload = await _context.TabMessageResponsePayloads.FindAsync(payloadId);
            if (payload == null)
            {
                _logger.LogError("Payload ID {Id} not found.", payloadId);
                return;
            }

            try
            {
                payload.Status = "PROCESSING";
                payload.UpdatedAt = DateTime.Now;
                payload.UpdatedBy = "BACKGROUND";
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                await _logService.LogExceptionAsync(ex, _logInstancePath);
                _logger.LogError(ex, "Failed to update payload status to PROCESSING for ID {Id}", payloadId);
                return;
            }

            // 2. Start Transaction Logic
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 3. Insert New Submission (Duplicate check is now handled at Controller level)
                    var submission = new TabMessageResponseSubmisison
                    {
                        ResponseType = request.DocumentControl.ResponseInfo.Status,
                        ReferenceNumber = request.DocumentControl.ReferenceNumber,
                        DocumentNumber = request.DocumentControl.DocumentNumber,
                        MessageType = request.DocumentControl.MessageType ?? "",
                        ResponseCode = request.DocumentControl.ResponseInfo.Code,
                        ResponseMessage = request.DocumentControl.Remark ?? "",
                        ResponseDateTime = request.DocumentControl.ResponseInfo.DateTime,
                        RegistrationId = "",
                        ResponseToId = request.DocumentControl.ReferenceNumber,
                        QueueStatus = "WAIT",
                        SystemTime = DateTime.Now,
                        ResponsePayloadId = payloadId,
                        FlagUpdate = "N",
                        CreatedAt = DateTime.Now,
                        CreatedBy = "BACKGROUND"
                    };

                    _context.TabMessageResponseSubmisisons.Add(submission);
                    _logger.LogInformation("Creating new submission for Doc: {Doc}", request.DocumentControl.DocumentNumber);

                    await _context.SaveChangesAsync();

                    // 4. Create Record txn_outbounds (KeyId = response_submission.Id, TxnType = EPC-0201, Status = WAIT)
                    var outbound = new TabMessageTxnOutbound
                    {
                        KeyId = submission.Id,
                        TxnType = "EPC-0201",
                        Description = $"Process ePhyto Progress Ref: {request.DocumentControl.ReferenceNumber}",
                        Status = "WAIT",
                        CreatedAt = DateTime.Now,
                        CreatedBy = "BACKGROUND"
                    };

                    _context.TabMessageTxnOutbounds.Add(outbound);

                    // 5. Update Record response_payload (Status = SUCCESS)
                    payload.Status = "SUCCESS";
                    payload.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                    
                    // 6. Commit Transaction
                    await transaction.CommitAsync();

                    _logger.LogInformation("Background processing SUCCESS for Ref: {Ref} (Submission ID: {SubId})", 
                        request.DocumentControl.ReferenceNumber, submission.Id);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    
                    // บันทึก Log ลงไฟล์ JSON ผ่าน LogService
                    await _logService.LogExceptionAsync(ex, _logInstancePath);
                    
                    _logger.LogError(ex, "Background processing FAILED for Ref: {Ref}", request.DocumentControl.ReferenceNumber);

                    // 7. Update Record response_payload (Status = FAIL)
                    // สำคัญ: ต้อง Clear tracking เพื่อไม่ให้ตอน Save FAIL มันพยายามไปเซฟข้อมูลที่พังซ้ำอีกรอบ
                    _context.ChangeTracker.Clear();

                    try
                    {
                        var failPayload = await _context.TabMessageResponsePayloads.FindAsync(payloadId);
                        if (failPayload != null)
                        {
                            failPayload.Status = "FAIL";
                            failPayload.UpdatedAt = DateTime.Now;
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch (Exception logEx)
                    {
                        await _logService.LogExceptionAsync(logEx, _logInstancePath);
                        _logger.LogError(logEx, "Failed to mark payload as FAIL for ID {Id}", payloadId);
                    }
                }
            });
        }
        public async Task<bool> IsDocumentNumberDuplicateAsync(string documentNumber)
        {
            if (string.IsNullOrEmpty(documentNumber)) return false;
            
            return await _context.TabMessageResponseSubmisisons
                .AnyAsync(s => s.DocumentNumber == documentNumber);
        }
    }
}
