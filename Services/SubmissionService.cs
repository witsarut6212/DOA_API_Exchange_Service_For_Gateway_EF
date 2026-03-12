using DOA_API_Exchange_Service_For_Gateway.Data;
using DOA_API_Exchange_Service_For_Gateway.Models.Entities;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;
using DOA_API_Exchange_Service_For_Gateway.Helpers;
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

        public async Task<int> SaveResponsePayloadAsync(string rawDataObject, string source, string? docId = null)
        {
            _context.CurrentUser = source;
            try
            {
                var payload = new TabMessageResponsePayload
                {
                    Status = ApiConstants.PayloadStatus.Wait,
                    DataObject = rawDataObject
                    // CreatedAt/By handled automatically by AppDbContext
                };

                _context.TabMessageResponsePayloads.Add(payload);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Step 1: Saved payload for Ref: {Ref} (ID: {Id})", 
                    docId, payload.Id);
                
                return payload.Id;
            }
            catch (Exception ex)
            {
                await _logService.LogExceptionAsync(ex, _logInstancePath);
                _logger.LogError(ex, "Step 1 Failed: Error saving payload for Ref: {Ref}", 
                    docId);
                return 0;
            }
        }

        public async Task ProcessPayloadAsync(int payloadId, EPhytoProgressRequest request, string source)
        {
            _context.CurrentUser = source;
            var payload = await _context.TabMessageResponsePayloads.FindAsync(payloadId);
            if (payload == null)
            {
                _logger.LogError("Payload ID {Id} not found.", payloadId);
                return;
            }

            try
            {
                payload.Status = ApiConstants.PayloadStatus.Processing;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                await _logService.LogExceptionAsync(ex, _logInstancePath);
                _logger.LogError(ex, "Failed to update payload status to PROCESSING for ID {Id}", payloadId);
                return;
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var submission = CreateSubmissionRecord(payloadId, request, source);
                    _context.TabMessageResponseSubmissions.Add(submission);
                    _logger.LogInformation("Creating new submission for Doc: {Doc}", request.DocumentControl.DocumentNumber);

                    await _context.SaveChangesAsync();

                    var outbound = CreateOutboundRecord(submission.Id, request.DocumentControl.ReferenceNumber, source);
                    _context.TabMessageTxnOutbounds.Add(outbound);

                    payload.Status = ApiConstants.PayloadStatus.Success;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Background processing SUCCESS for Ref: {Ref} (Submission ID: {SubId})", 
                        request.DocumentControl.ReferenceNumber, submission.Id);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await _logService.LogExceptionAsync(ex, _logInstancePath);
                    _logger.LogError(ex, "Background processing FAILED for Ref: {Ref}", request.DocumentControl.ReferenceNumber);

                    _context.ChangeTracker.Clear();

                    try
                    {
                        var failPayload = await _context.TabMessageResponsePayloads.FindAsync(payloadId);
                        if (failPayload != null)
                        {
                            failPayload.Status = ApiConstants.PayloadStatus.Fail;
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
            
            return await _context.TabMessageResponseSubmissions
                .AnyAsync(s => s.DocumentNumber == documentNumber);
        }

        public async Task<int> SaveCertificatePayloadAsync(string rawDataObject, string source, string referenceNumber)
        {
            _context.CurrentUser = source;

            try
            {
                var payload = new TabMessageResponsePayload
                {
                    Status = ApiConstants.PayloadStatus.Wait,
                    DataObject = rawDataObject
                };

                _context.TabMessageResponsePayloads.Add(payload);
                await _context.SaveChangesAsync();

                var outbound = new TabMessageTxnOutbound
                {
                    KeyId = payload.Id,
                    TxnType = ApiConstants.TxnType.EPhytoCertificate,
                    Description = $"Process ePhyto Certificate Ref: {referenceNumber}",
                    Status = ApiConstants.QueueStatus.Wait
                };

                _context.TabMessageTxnOutbounds.Add(outbound);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Saved certificate payload and outbound for Ref: {Ref} (PayloadId: {PayloadId}, OutboundId: {OutboundId})",
                    referenceNumber,
                    payload.Id,
                    outbound.Id);

                return payload.Id;
            }
            catch (Exception ex)
            {
                await _logService.LogExceptionAsync(ex, _logInstancePath);
                _logger.LogError(
                    ex,
                    "Error saving certificate payload/outbound for Ref: {Ref}",
                    referenceNumber);

                return 0;
            }
        }

        public async Task<int> SavePqCertificatePayloadAsync(string rawDataObject, string source, string referenceNumber)
        {
            _context.CurrentUser = source;

            // Optional: เช็ค CanEditCertificateAsync ตรงนี้เลยก็ได้ถ้าต้องการคุมที่ระดับ Service
            // if (!await CanEditCertificateAsync(referenceNumber)) { return -1; }

            try
            {
                var payload = new TabMessageResponsePayload
                {
                    Status = ApiConstants.PayloadStatus.Wait,
                    DataObject = rawDataObject
                };

                _context.TabMessageResponsePayloads.Add(payload);
                await _context.SaveChangesAsync();

                var outbound = new TabMessageTxnOutbound
                {
                    KeyId = payload.Id,
                    TxnType = ApiConstants.TxnType.PqCertificate,
                    Description = $"Process PQ Certificate Ref: {referenceNumber}",
                    Status = ApiConstants.QueueStatus.Wait
                };

                _context.TabMessageTxnOutbounds.Add(outbound);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Saved PQ certificate payload and outbound for Ref: {Ref} (PayloadId: {PayloadId}, OutboundId: {OutboundId})",
                    referenceNumber,
                    payload.Id,
                    outbound.Id);

                return payload.Id;
            }
            catch (Exception ex)
            {
                await _logService.LogExceptionAsync(ex, _logInstancePath);
                _logger.LogError(
                    ex,
                    "Error saving PQ certificate payload/outbound for Ref: {Ref}",
                    referenceNumber);

                return 0;
            }
        }

        public async Task<bool> CanEditCertificateAsync(string referenceNumber)
        {
            if (string.IsNullOrEmpty(referenceNumber)) return true; // ถ้าไม่มีเลข อนุมัติให้เป็นใบใหม่

            // เช็คใน TabMessageThphytos ว่ามีเลขนี้อยู่แล้วหรือยัง
            // หมายเหตุ: DocStatus สำหรับ Draft ปกติจะเป็น '01' หรือ 'Draft' ขึ้นอยู่กับระบบ
            // ในที่นี้เราเช็คว่าถ้ามีแล้ว ต้องเป็นสถานะที่ยังแก้ไขได้
            var existing = await _context.TabMessageThphytos
                .Where(t => t.DocId == referenceNumber)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            if (existing == null) return true; // ยังไม่มีข้อมูล แก้ไขได้ (เป็นใบใหม่)

            // ยอมให้แก้ถ้า DocStatus เป็น '01' (Draft) หรือ 'Draft'
            return existing.DocStatus == "01" || existing.DocStatus.Equals("Draft", StringComparison.OrdinalIgnoreCase);
        }

        #region Private Helper Methods for Entity Mapping

        private TabMessageResponseSubmission CreateSubmissionRecord(int payloadId, EPhytoProgressRequest request, string source)
        {
            return new TabMessageResponseSubmission
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
                QueueStatus = ApiConstants.QueueStatus.Wait,
                SystemTime = DateTime.Now,
                ResponsePayloadId = payloadId,
                FlagUpdate = ApiConstants.CommonStatus.No
                // CreatedAt/By handled automatically
            };
        }

        private TabMessageTxnOutbound CreateOutboundRecord(int submissionId, string referenceNumber, string source)
        {
            return new TabMessageTxnOutbound
            {
                KeyId = submissionId,
                TxnType = ApiConstants.TxnType.EPhytoProgress,
                Description = $"Process ePhyto Progress Ref: {referenceNumber}",
                Status = ApiConstants.QueueStatus.Wait
                // CreatedAt/By handled automatically
            };
        }

        #endregion
    }
}
