using DOA_API_Exchange_Service_For_Gateway.Data;
using DOA_API_Exchange_Service_For_Gateway.Models.Entities;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;
using DOA_API_Exchange_Service_For_Gateway.Helpers;
using Microsoft.EntityFrameworkCore;

namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    public class EPhytoService : IEPhytoService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EPhytoService> _logger;
        private readonly ILogService _logService;
        private readonly IConfiguration _configuration;

        public EPhytoService(AppDbContext context, ILogger<EPhytoService> logger, ILogService logService, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _logService = logService;
            _configuration = configuration;
        }

        public async Task<bool> IsDocumentExists(string docType, string docStatus, string docId)
        {
            return await _context.TabMessageThphytos.AnyAsync(t => t.DocType == docType && t.DocStatus == docStatus && t.DocId == docId);
        }

        public async Task<int> SaveEPhytoPayloadAsync(string rawDataObject, string source, string systemOrigin, string? docId = null)
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

                _logger.LogInformation("Step 1 [{Source}]: Saved payload for DocId: {DocId} (PayloadId: {Id})",
                    source, docId, payload.Id);

                return payload.Id;
            }
            catch (Exception ex)
            {
                var instancePath = _configuration["ApiSettings:RoutePrefix"] ?? "UNKNOWN";
                await _logService.LogExceptionAsync(ex, instancePath);
                _logger.LogError(ex, "Step 1 [{Source}] Failed: Error saving payload for DocId: {DocId}",
                    source, docId);
                return 0;
            }
        }

        public async Task ProcessEPhytoPayloadAsync(int payloadId, EPhytoRequest request, string source, string systemOrigin)
        {
            _context.CurrentUser = source;
            var instancePath = _configuration["ApiSettings:RoutePrefix"] ?? "UNKNOWN";

            var payload = await _context.TabMessageResponsePayloads.FindAsync(payloadId);
            if (payload == null)
            {
                _logger.LogError("[{Source}] Payload ID {Id} not found.", source, payloadId);
                return;
            }

            try
            {
                payload.Status = ApiConstants.PayloadStatus.Processing;
                // UpdatedAt/By handled automatically by AppDbContext
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                await _logService.LogExceptionAsync(ex, instancePath);
                _logger.LogError(ex, "[{Source}] Failed to update payload to PROCESSING for ID {Id}", source, payloadId);
                return;
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    string messageId = Guid.NewGuid().ToString();

                    var thphyto = MapToThPhyto(request, messageId, source, systemOrigin);
                    _context.TabMessageThphytos.Add(thphyto);
                    await _context.SaveChangesAsync();

                    MapIncludedNotes(request.XcDocument.IncludeNotes, messageId);
                    MapReferenceDocs(request, messageId);
                    MapTransportInfo(request.Consignment, messageId);
                    MapItems(request.Items, messageId);

                    _context.TabMessageTxnOutbounds.Add(new TabMessageTxnOutbound
                    {
                        KeyId       = thphyto.Id,
                        TxnType     = ApiConstants.TxnType.EPhytoSubmission,
                        Description = "",
                        Status      = ApiConstants.QueueStatus.Wait
                        // CreatedAt/By handled automatically by AppDbContext
                    });

                    payload.Status = ApiConstants.PayloadStatus.Success;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("[{Source}] Background processing SUCCESS for DocId: {DocId} (MessageId: {MessageId}, ThphytoId: {ThId})",
                        source, request.XcDocument?.DocId, messageId, thphyto.Id);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await _logService.LogExceptionAsync(ex, instancePath);
                    _logger.LogError(ex, "[{Source}] Background processing FAILED for DocId: {DocId}", source, request.XcDocument?.DocId);

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
                        await _logService.LogExceptionAsync(logEx, instancePath);
                        _logger.LogError(logEx, "[{Source}] Failed to mark payload FAIL for ID {Id}", source, payloadId);
                    }
                }
            });
        }

        #region Mapping Helpers (Private Methods)

        private TabMessageThphyto MapToThPhyto(EPhytoRequest request, string messageId, string source, string systemOrigin)
        {
            var doc = request.XcDocument;
            var consignment = request.Consignment;

            string? authLocationName = null;
            if (doc.SignatoryAuthen?.IssueLocation != null)
            {
                if (doc.SignatoryAuthen.IssueLocation is string locStr)
                {
                    authLocationName = locStr;
                }
                else if (doc.SignatoryAuthen.IssueLocation is Newtonsoft.Json.Linq.JObject locObj)
                {
                    authLocationName = locObj["name"]?.ToString();
                }
            }

            return new TabMessageThphyto
            {
                MessageId = messageId,
                MessageStatus = "NEW",
                PhytoTo = systemOrigin,
                DocName = doc.DocName,
                DocId = doc.DocId,
                DocType = doc.DocType,
                DocStatus = doc.StatusCode,
                IssueDateTime = DateTime.TryParse(doc.IssueDate, out var dt) ? dt : DateTime.Now,
                IssuerName = doc.IssuePartyName ?? "N/A",
                RequestDateTime = DateTime.Now,
                ConsignorName = consignment.ConsignorParty?.Name ?? "N/A",
                ConsignorAddrLine1 = consignment.ConsignorParty?.AddressLine1,
                ConsigneeName = consignment.ConsigneeParty?.Name ?? "N/A",
                ConsigneeAddrLine1 = consignment.ConsigneeParty?.AddressLine1,
                ExportCountryId = consignment.ExportCountry?.Id ?? "",
                ImportCountryId = consignment.ImportCountry?.Id ?? "",
                UnloadingBasePortName = consignment.UnloadingBasePort?.Name,
                AuthLocationName = authLocationName,
                AuthProviderName = doc.SignatoryAuthen?.ProviderParty?.Name,
                AuthActualDateTime = doc.SignatoryAuthen?.ActualDatetime,
                ResponseStatus = "0101",
                TimeStamp = DateTime.Now,
                LastUpdate = DateTime.Now,
                QueueStatus = ApiConstants.QueueStatus.InQueue,
                UserId = systemOrigin
            };
        }

        private void MapIncludedNotes(List<IncludeNote>? notes, string messageId)
        {
            if (notes == null) return;

            foreach (var hn in notes)
            {
                string contentStr = hn.Contents != null 
                    ? string.Join(", ", hn.Contents.Select(c => c.Content)) 
                    : "";
                
                _context.TabMessageThphytoIncludedNotes.Add(new TabMessageThphytoIncludedNote 
                { 
                    MessageId = messageId, 
                    Subject = hn.Subject ?? "N/A", 
                    Content = contentStr
                    // CreatedAt handled automatically
                });
            }
        }

        private void MapReferenceDocs(EPhytoRequest request, string messageId)
        {
            var allRefs = new List<ReferenceDocRequest>();
            if (request.XcDocument.ReferenceDocs != null) allRefs.AddRange(request.XcDocument.ReferenceDocs);
            if (request.PhytoCerts != null) allRefs.AddRange(request.PhytoCerts);

            foreach (var r in allRefs)
            {
                _context.TabMessageThphytoReferenceDocs.Add(new TabMessageThphytoReferenceDoc
                {
                    MessageId = messageId,
                    DocId = request.XcDocument.DocId,
                    RefDocId = r.DocId ?? r.DocumentNo ?? "",
                    Filename = r.Filename ?? r.Name,
                    PdfObject = r.PdfObject
                    // CreatedAt handled automatically
                });
            }
        }

        private void MapTransportInfo(Consignment consignment, string messageId)
        {
            if (consignment.UtilizeTransport != null)
            {
                foreach (var ut in consignment.UtilizeTransport)
                {
                    _context.TabMessageThphytoUtilizeTransports.Add(new TabMessageThphytoUtilizeTransport
                    {
                        MessageId = messageId,
                        SealNumber = ut.SealNumber
                        // CreatedAt handled automatically
                    });
                }
            }

            if (consignment.MainCarriages != null)
            {
                foreach (var mc in consignment.MainCarriages)
                {
                    _context.TabMessageThphytoMainCarriages.Add(new TabMessageThphytoMainCarriage
                    {
                        MessageId = messageId,
                        TransportModeCode = mc.ModeCode,
                        TransportMeanName = mc.TransportMeanName
                        // CreatedAt handled automatically
                    });
                }
            }
        }

        private void MapItems(List<EPhytoItem> items, string messageId)
        {
            foreach (var item in items)
            {
                string itemId = Guid.NewGuid().ToString();
                _context.TabMessageThphytoItems.Add(new TabMessageThphytoItem
                {
                    MessageId = messageId,
                    ItemId = itemId,
                    SequenceNo = int.TryParse(item.SequenceNo, out var seq) ? seq : 0,
                    ProductScientName = item.ScientName
                    // CreatedAt handled automatically
                });

                MapItemDetails(item, messageId, itemId);
            }
        }

        private void MapItemDetails(EPhytoItem item, string messageId, string itemId)
        {
            if (item.Descriptions != null)
            {
                foreach (var d in item.Descriptions)
                {
                    _context.TabMessageThphytoItemDescriptions.Add(new TabMessageThphytoItemDescription
                    {
                        MessageId = messageId,
                        ItemId = itemId,
                        ProductDescription = d.Name ?? ""
                        // CreatedAt handled automatically
                    });
                }
            }

            if (item.CommonNames != null)
            {
                foreach (var c in item.CommonNames)
                {
                    _context.TabMessageThphytoItemCommonNames.Add(new TabMessageThphytoItemCommonName
                    {
                        MessageId = messageId,
                        ItemId = itemId,
                        ProudctCommonName = c.Name ?? ""
                        // CreatedAt handled automatically
                    });
                }
            }

            MapItemAdditionalNotes(item.AdditionalNotes, messageId, itemId);
        }

        private void MapItemAdditionalNotes(List<IncludeNote>? notes, string messageId, string itemId)
        {
            if (notes == null) return;

            foreach (var n in notes)
            {
                string additionalNoteId = Guid.NewGuid().ToString();
                _context.TabMessageThphytoItemAdditionalNotes.Add(new TabMessageThphytoItemAdditionalNote
                {
                    MessageId = messageId,
                    ItemId = itemId,
                    AdditionalNoteId = additionalNoteId,
                    Subject = n.Subject ?? "N/A"
                    // CreatedAt handled automatically
                });

                if (n.Contents != null)
                {
                    foreach (var c in n.Contents)
                    {
                        _context.TabMessageThphytoItemAdditionalNoteContents.Add(new TabMessageThphytoItemAdditionalNoteContent
                        {
                            MessageId = messageId,
                            ItemId = itemId,
                            AdditionalNoteId = additionalNoteId,
                            NoteContent = c.Content ?? ""
                            // CreatedAt handled automatically
                        });
                    }
                }
            }
        }

        #endregion
    }
}
