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
        private readonly ICertificateQueue _certificateQueue;
        private readonly string _logProgressInstancePath;
        private readonly string _logCertInstancePath;

        public SubmissionService(
            AppDbContext context, 
            ILogger<SubmissionService> logger, 
            ILogService logService, 
            IConfiguration configuration,
            ICertificateQueue certificateQueue)
        {
            _context = context;
            _logger = logger;
            _logService = logService;
            _configuration = configuration;
            _certificateQueue = certificateQueue;
            _logProgressInstancePath = _configuration["ApiSettings:SubmissionProgressPath"] ?? "/submission/ephyto/progress";
            _logCertInstancePath = _configuration["ApiSettings:SubmissionCertificatePath"] ?? "/submission/ephyto/certificate";
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
                await _logService.LogExceptionAsync(ex, _logProgressInstancePath);
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
                await _logService.LogExceptionAsync(ex, _logProgressInstancePath);
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
                    await _logService.LogExceptionAsync(ex, _logProgressInstancePath);
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
                        await _logService.LogExceptionAsync(logEx, _logProgressInstancePath);
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

        public async Task<int> SaveCertificatePayloadAsync(string rawDataObject, string source, EPhytoCertificateRequest request)
        {
            _context.CurrentUser = source;
            var referenceNumber = request.DocumentControl.ReferenceNumber;

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
                    Description = $"Process Certificate Ref: {referenceNumber}",
                    Status = ApiConstants.QueueStatus.Wait
                };

                _context.TabMessageTxnOutbounds.Add(outbound);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Step 1 [{Source}]: Saved certificate payload and outbound for Ref: {Ref} (PayloadID: {PayloadId}, OutboundID: {OutboundId})",
                    source,
                    referenceNumber,
                    payload.Id,
                    outbound.Id);

                // Enqueue to background service
                _certificateQueue.Enqueue(payload.Id, request, source);

                return payload.Id;
            }
            catch (Exception ex)
            {
                await _logService.LogExceptionAsync(ex, _logCertInstancePath);
                _logger.LogError(
                    ex,
                    "Step 1 [{Source}] Failed: Error saving certificate payload/outbound for Ref: {Ref}",
                    source,
                    referenceNumber);

                return 0;
            }
        }


        public async Task ProcessCertificatePayloadAsync(int payloadId, EPhytoCertificateRequest request, string source)
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
                await _logService.LogExceptionAsync(ex, _logCertInstancePath);
                _logger.LogError(ex, "[{Source}] Failed to update certificate payload status to PROCESSING for ID {Id}", source, payloadId);
                return;
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Update Transaction Status in TabMessageTxnOutbound to PROCESSING
                    var outbound = await _context.TabMessageTxnOutbounds
                        .FirstOrDefaultAsync(o => o.KeyId == payloadId && o.TxnType == ApiConstants.TxnType.EPhytoCertificate);
                    
                    if (outbound != null)
                    {
                        outbound.Status = ApiConstants.QueueStatus.InQueue;
                    }

                    // --- Record into TabMessageResponseSubmissions ---
                    // Always create a new record as per updated spec
                    var referenceNumber = request.DocumentControl.ReferenceNumber;
                    var submission = CreateCertificateSubmissionRecord(payloadId, request, source);
                    _context.TabMessageResponseSubmissions.Add(submission);

                    _logger.LogInformation("[{Source}] Creating new certificate submission record for Ref: {Ref} (ID: {Id})", source, referenceNumber, submission.Id);


                    await _context.SaveChangesAsync();
                    
                    // In a real scenario, here we might map to ThPhyto and other tables (Complex mapping)
                    // For now, we fulfill the requirement of tracking the submission status.
                    
                    payload.Status = ApiConstants.PayloadStatus.Success;
                    if (outbound != null) outbound.Status = ApiConstants.QueueStatus.Success;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("[{Source}] Certificate processing SUCCESS for Ref: {Ref} (PayloadId: {Id})", 
                        source, request.DocumentControl.ReferenceNumber, payloadId);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await _logService.LogExceptionAsync(ex, _logCertInstancePath);
                    string errorDetail = ex.InnerException != null ? ($" | Inner Error: " + ex.InnerException.Message) : "";
                    _logger.LogError(ex, "[{Source}] Certificate processing FAILED for Ref: {Ref}{ErrorDetail}", source, request.DocumentControl.ReferenceNumber, errorDetail);

                    _context.ChangeTracker.Clear();
                    try
                    {
                        var failPayload = await _context.TabMessageResponsePayloads.FindAsync(payloadId);
                        if (failPayload != null) failPayload.Status = ApiConstants.PayloadStatus.Fail;
                        
                        var failOutbound = await _context.TabMessageTxnOutbounds
                            .FirstOrDefaultAsync(o => o.KeyId == payloadId && o.TxnType == ApiConstants.TxnType.EPhytoCertificate);
                        if (failOutbound != null) failOutbound.Status = ApiConstants.QueueStatus.Fail;

                        await _context.SaveChangesAsync();
                    }
                    catch { }
                }
            });
        }


        public async Task<bool> CanEditCertificateAsync(string referenceNumber)
        {
            if (string.IsNullOrEmpty(referenceNumber)) return true; // ถ้าไม่มีเลข อนุมัติให้เป็นใบใหม่

            // เช็คใน TabMessageThphytos ว่ามีเลขนี้อยู่แล้วหรือยัง
            // หมายเหตุ: DocStatus สำหรับ Draft ปกติจะเป็น '01' หรือ 'Draft' ขึ้นอยู่กับระบบ
            // ในที่นี้เราเช็คว่าถ้ามีแล้ว ต้องเป็นสถานะที่ยังแก้ไขได้
            var existing = await _context.TabMessageThphytos
                .Where(t => t.DocId == referenceNumber)
                .OrderByDescending(t => t.TimeStamp)
                .FirstOrDefaultAsync();

            if (existing == null) return true; // ยังไม่มีข้อมูล แก้ไขได้ (เป็นใบใหม่)

            // ยอมให้แก้ถ้า DocStatus เป็น '01' (Draft) หรือ 'Draft'
            return existing.DocStatus == "01" || existing.DocStatus.Equals("Draft", StringComparison.OrdinalIgnoreCase);
        }

        #region Private Helper Methods for Entity Mapping

        private TabMessageResponseSubmission CreateSubmissionRecord(int payloadId, EPhytoProgressRequest request, string source)
        {
            var docControl = request.DocumentControl;
            return new TabMessageResponseSubmission
            {
                ResponseType = docControl.ResponseInfo.Status,
                ReferenceNumber = docControl.ReferenceNumber,
                // If DocumentNumber is not provided, generate a unique one as we do for certificates
                DocumentNumber = docControl.DocumentNumber ?? Guid.NewGuid().ToString("N").Substring(0, 20).ToUpper(),
                MessageType = docControl.MessageType,
                ResponseCode = docControl.ResponseInfo.Code,
                ResponseMessage = docControl.Remark,
                ResponseDateTime = docControl.ResponseInfo.DateTime,
                RegistrationId = docControl.RegistrationId,
                ResponseToId = docControl.ReferenceNumber,
                QueueStatus = ApiConstants.QueueStatus.Wait,
                SystemTime = DateTime.Now,
                ResponsePayloadId = payloadId,
                FlagUpdate = "NEW",
                MarkSend = "N"
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

        private TabMessageResponseSubmission CreateCertificateSubmissionRecord(int payloadId, EPhytoCertificateRequest request, string source)
        {
            var docControl = request.DocumentControl;
            return new TabMessageResponseSubmission
            {
                ResponseType = docControl.CertificateStatus ?? "DRAFT",
                ReferenceNumber = docControl.ReferenceNumber,
                // Using a unique ID for DocumentNumber to allow history of submissions for the same ReferenceNumber
                DocumentNumber = Guid.NewGuid().ToString("N").Substring(0, 20).ToUpper(),
                MessageType = "PHYTOCERT",
                ResponseCode = "DRAFT",
                ResponseMessage = $"Submission of {docControl.FormType?.ToUpper() ?? "Certificate"} as DRAFT",
                ResponseDateTime = DateTime.Now,
                RegistrationId = docControl.RegistrationID ?? "",
                ResponseToId = docControl.ReferenceNumber,
                QueueStatus = ApiConstants.QueueStatus.Wait,
                SystemTime = DateTime.Now,
                ResponsePayloadId = payloadId,
                FlagUpdate = "NEW",
                MarkSend = "N"
            };
        }



        #endregion
    }
}
