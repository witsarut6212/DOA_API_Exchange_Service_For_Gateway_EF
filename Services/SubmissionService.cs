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

        public async Task<int> SaveResponsePayloadAsync(EPhytoProgressRequest request)
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

                _logger.LogInformation("Step 1: Saved payload for Ref: {Ref} (ID: {Id})", 
                    request.DocumentControl.ReferenceNumber, payload.Id);
                
                return payload.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Step 1 Failed: Error saving payload for Ref: {Ref}", 
                    request.DocumentControl.ReferenceNumber);
                return 0;
            }
        }

        public async Task ProcessPayloadAsync(int payloadId, EPhytoProgressRequest request)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var referenceNo = request.DocumentControl.ReferenceNumber;
                    var newStatus = request.DocumentControl.ResponseInfo.Status;
                    var updateTime = request.DocumentControl.ResponseInfo.DateTime;

                    // Update TabMessageThphyto
                    var thphyto = await _context.TabMessageThphytos
                        .FirstOrDefaultAsync(x => x.DocId == referenceNo || x.MessageId == referenceNo);

                    string processingResult = "NOT_FOUND";
                    if (thphyto != null)
                    {
                        thphyto.MessageStatus = newStatus;
                        thphyto.ResponseStatus = request.DocumentControl.ResponseInfo.Code;
                        thphyto.ResponseAt = updateTime;
                        thphyto.LastUpdate = DateTime.Now;
                        processingResult = "SUCCESS";
                    }

                    // Update Payload Status
                    var payload = await _context.TabMessageRepsonsePayloads.FindAsync(payloadId);
                    if (payload != null)
                    {
                        payload.Status = processingResult;
                        payload.UpdatedAt = DateTime.Now;
                        payload.UpdatedBy = "BACKGROUND";
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Step 2: Processed Ref: {Ref} (Result: {Result})", referenceNo, processingResult);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Step 2 Failed: Processing Ref: {Ref}", request.DocumentControl.ReferenceNumber);
                    
                    // Mark payload as FAIL
                    var payload = await _context.TabMessageRepsonsePayloads.FindAsync(payloadId);
                    if (payload != null)
                    {
                        payload.Status = "FAIL";
                        payload.UpdatedAt = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                }
            });
        }
    }
}
