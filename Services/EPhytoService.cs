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
        private readonly IPreSaveLogger _preSaveLogger;
        private readonly IConfiguration _configuration;

        public EPhytoService(AppDbContext context, ILogger<EPhytoService> logger, ILogService logService, IPreSaveLogger preSaveLogger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _logService = logService;
            _preSaveLogger = preSaveLogger;
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

        public async Task ProcessEPhytoPayloadAsync(int payloadId, object request, string source, string systemOrigin)
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
                    TabMessageThphyto thphyto;
                    string? docIdForLog = null;

                    if (systemOrigin == "ASW" && request is AswNormalRequest aswReq)
                    {
                        docIdForLog = aswReq.DocId;
                        thphyto = MapToThPhytoFromAsw(aswReq, messageId, source);

                        await _preSaveLogger.LogAsync(docIdForLog, "ASW_Normal", messageId, new { Header = thphyto, Request = aswReq });

                        _context.TabMessageThphytos.Add(thphyto);
                        await _context.SaveChangesAsync();

                        MapIncludedNotesFromAsw(aswReq.Notes, messageId);
                        MapItemsFromAsw(aswReq.Detail, messageId);
                        // Add more ASW mapping if needed
                    }
                    else if (request is IppcRequest ippcReq)
                    {
                        docIdForLog = ippcReq.XcDocument?.DocId;
                        var apiName = "IPPC_Normal";
                        if (ippcReq.XcDocument != null)
                        {
                            if (ippcReq.XcDocument.DocType == "657") apiName = "IPPC_Reexport";
                            else if (ippcReq.XcDocument.DocType == "851" && ippcReq.XcDocument.StatusCode == "40") apiName = "IPPC_Withdraw";
                        }
                        
                        thphyto = MapToThPhytoFromIppc(ippcReq, messageId, source, systemOrigin);

                        await _preSaveLogger.LogAsync(docIdForLog, apiName, messageId, new { Header = thphyto, Request = ippcReq });

                        _context.TabMessageThphytos.Add(thphyto);
                        await _context.SaveChangesAsync();

                        MapIncludedNotesFromIppc(ippcReq.XcDocument?.IncludeNotes, messageId);
                        MapReferenceDocsFromIppc(ippcReq, messageId);
                        MapTransportInfoFromIppc(ippcReq.Consignment, messageId);
                        MapItemsFromIppc(ippcReq.Items, messageId);
                    }
                    else
                    {
                        throw new Exception($"Unknown request type or origin mismatch: {systemOrigin}");
                    }

                    _context.TabMessageTxnOutbounds.Add(new TabMessageTxnOutbound
                    {
                        KeyId = thphyto.Id,
                        TxnType = systemOrigin == "ASW" ? ApiConstants.TxnType.EPhytoASW : ApiConstants.TxnType.EPhytoIPPC,
                        Description = "",
                        Status = ApiConstants.QueueStatus.Wait
                    });

                    payload.Status = ApiConstants.PayloadStatus.Success;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("[{Source}] Background processing SUCCESS for DocId: {DocId} (MessageId: {MessageId}, ThphytoId: {ThId})",
                        source, docIdForLog, messageId, thphyto.Id);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await _logService.LogExceptionAsync(ex, instancePath);
                    _logger.LogError(ex, "[{Source}] Background processing FAILED", source);

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

        #region Mapping Helpers (IPPC)

        private TabMessageThphyto MapToThPhytoFromIppc(IppcRequest request, string messageId, string source, string systemOrigin)
        {
            var doc = request.XcDocument;
            var consignment = request.Consignment;

            string? authLocationName = null;
            string? authLocationId = null;
            if (doc.SignatoryAuthen?.IssueLocation != null)
            {
                if (doc.SignatoryAuthen.IssueLocation is string locStr) authLocationName = locStr;
                else if (doc.SignatoryAuthen.IssueLocation is Newtonsoft.Json.Linq.JObject locObj)
                {
                    authLocationName = locObj["name"]?.ToString();
                    authLocationId = locObj["id"]?.ToString();
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
                IssuerId = doc.IssuePartyId,
                IssuerName = doc.IssuePartyName ?? "N/A",
                RequestDateTime = DateTime.Now,
                ConsignorId = consignment.ConsignorParty?.Id,
                ConsignorName = consignment.ConsignorParty?.Name ?? "N/A",
                ConsignorAddrLine1 = consignment.ConsignorParty?.AddressLine1,
                ConsignorCityName = consignment.ConsignorParty?.CityName,
                ConsignorPostcode = consignment.ConsignorParty?.Postcode,
                ConsigneeId = consignment.ConsigneeParty?.Id,
                ConsigneeName = consignment.ConsigneeParty?.Name ?? "N/A",
                ConsigneeAddrLine1 = consignment.ConsigneeParty?.AddressLine1,
                ConsigneeCityName = consignment.ConsigneeParty?.CityName,
                ConsigneePostcode = consignment.ConsigneeParty?.Postcode,
                ExportCountryId = consignment.ExportCountry?.Id ?? "",
                ExportCountryName = consignment.ExportCountry?.Name,
                ImportCountryId = consignment.ImportCountry?.Id ?? "",
                ImportCountryName = consignment.ImportCountry?.Name,
                UnloadingBasePortId = consignment.UnloadingBasePort?.Id,
                UnloadingBasePortName = consignment.UnloadingBasePort?.Name,
                AuthLocationId = authLocationId,
                AuthLocationName = authLocationName,
                AuthActualDateTime = doc.SignatoryAuthen?.ActualDatetime,
                AuthProviderId = doc.SignatoryAuthen?.ProviderParty?.Id,
                AuthProviderName = doc.SignatoryAuthen?.ProviderParty?.Name,
                AuthSpecifyPersonName = doc.SignatoryAuthen?.ProviderParty?.SpecifiedPerson?.Name,
                AuthAttainedQualificationName = doc.SignatoryAuthen?.ProviderParty?.SpecifiedPerson?.AttainedQualification?.Name,
                AuthAbbrevName = doc.SignatoryAuthen?.ProviderParty?.SpecifiedPerson?.AttainedQualification?.AbbrevName,
                ResponseStatus = "0101",
                TimeStamp = DateTime.Now,
                LastUpdate = DateTime.Now,
                QueueStatus = ApiConstants.QueueStatus.InQueue,
                UserId = systemOrigin
            };
        }

        private void MapIncludedNotesFromIppc(List<IncludeNote>? notes, string messageId)
        {
            if (notes == null) return;
            foreach (var hn in notes)
            {
                string contentStr = hn.Contents != null ? string.Join(", ", hn.Contents.Select(c => c.Content)) : "";
                _context.TabMessageThphytoIncludedNotes.Add(new TabMessageThphytoIncludedNote { MessageId = messageId, Subject = hn.Subject ?? "N/A", Content = contentStr });
            }
        }

        private void MapReferenceDocsFromIppc(IppcRequest request, string messageId)
        {
            var allRefs = new List<ReferenceDocRequest>();
            if (request.XcDocument.ReferenceDocs != null) allRefs.AddRange(request.XcDocument.ReferenceDocs);
            if (request.PhytoCerts != null) allRefs.AddRange(request.PhytoCerts);

            foreach (var r in allRefs)
            {
                _context.TabMessageThphytoReferenceDocs.Add(new TabMessageThphytoReferenceDoc { MessageId = messageId, DocId = request.XcDocument.DocId, RefDocId = r.DocId ?? r.DocumentNo ?? "", Filename = r.Filename ?? r.Name, PdfObject = r.PdfObject });
            }
        }

        private void MapTransportInfoFromIppc(Consignment consignment, string messageId)
        {
            if (consignment.UtilizeTransport != null)
                foreach (var ut in consignment.UtilizeTransport) _context.TabMessageThphytoUtilizeTransports.Add(new TabMessageThphytoUtilizeTransport { MessageId = messageId, SealNumber = ut.SealNumber });

            if (consignment.MainCarriages != null)
                foreach (var mc in consignment.MainCarriages) _context.TabMessageThphytoMainCarriages.Add(new TabMessageThphytoMainCarriage { MessageId = messageId, TransportModeCode = mc.ModeCode, TransportMeanName = mc.TransportMeanName });
        }

        private void MapItemsFromIppc(List<EPhytoItem> items, string messageId)
        {
            foreach (var item in items)
            {
                string itemId = Guid.NewGuid().ToString();
                _context.TabMessageThphytoItems.Add(new TabMessageThphytoItem { MessageId = messageId, ItemId = itemId, SequenceNo = int.TryParse(item.SequenceNo, out var seq) ? seq : 0, ProductScientName = item.ScientName });
                
                if (item.Descriptions != null)
                    foreach (var d in item.Descriptions) _context.TabMessageThphytoItemDescriptions.Add(new TabMessageThphytoItemDescription { MessageId = messageId, ItemId = itemId, ProductDescription = d.Name ?? "" });

                if (item.CommonNames != null)
                    foreach (var c in item.CommonNames) _context.TabMessageThphytoItemCommonNames.Add(new TabMessageThphytoItemCommonName { MessageId = messageId, ItemId = itemId, ProudctCommonName = c.Name ?? "" });

                if (item.AdditionalNotes != null)
                {
                    foreach (var n in item.AdditionalNotes)
                    {
                        string additionalNoteId = Guid.NewGuid().ToString();
                        _context.TabMessageThphytoItemAdditionalNotes.Add(new TabMessageThphytoItemAdditionalNote { MessageId = messageId, ItemId = itemId, AdditionalNoteId = additionalNoteId, Subject = n.Subject ?? "N/A" });
                        if (n.Contents != null)
                            foreach (var c in n.Contents) _context.TabMessageThphytoItemAdditionalNoteContents.Add(new TabMessageThphytoItemAdditionalNoteContent { MessageId = messageId, ItemId = itemId, AdditionalNoteId = additionalNoteId, NoteContent = c.Content ?? "" });
                    }
                }
            }
        }

        #endregion

        #region Mapping Helpers (ASW)

        private TabMessageThphyto MapToThPhytoFromAsw(AswNormalRequest request, string messageId, string source)
        {
            return new TabMessageThphyto
            {
                MessageId = messageId,
                MessageStatus = "NEW",
                PhytoTo = "ASW",
                DocName = request.DocName,
                DocId = request.DocId ?? "N/A",
                DocType = request.DocType ?? "",
                DocStatus = request.DocStatus ?? "",
                IssueDateTime = DateTime.TryParse(request.RefIssueDatetime, out var dt) ? dt : DateTime.Now,
                IssuerId = request.IssuerId,
                IssuerName = request.IssuerName ?? "N/A",
                RequestDateTime = DateTime.Now,
                ConsignorName = request.ExporterName ?? "N/A",
                ConsignorAddrLine1 = request.ExporterLine1,
                ConsigneeName = request.ConsigneeName ?? "N/A",
                ConsigneeAddrLine1 = request.ConsigneeLine1,
                ExportCountryId = request.ExportCountryCode ?? "",
                ExportCountryName = request.ExportCountryName,
                ImportCountryId = request.ImportCountryCode ?? "",
                ImportCountryName = request.ImportCountryName,
                UnloadingBasePortId = request.UnloadingPortCode,
                UnloadingBasePortName = request.UnloadingPortName,
                AuthLocationId = request.AuthLocationId,
                AuthLocationName = request.AuthLocationName,
                AuthProviderId = request.AuthProviderId,
                AuthProviderName = request.AuthProviderName,
                AuthSpecifyPersonName = request.AuthPerson,
                AuthAttainedQualificationName = request.AuthPositionName,
                AuthAbbrevName = request.AuthPositionAbb,
                AuthActualDateTime = null, // Will map from notes if needed
                ResponseStatus = "0101",
                TimeStamp = DateTime.Now,
                LastUpdate = DateTime.Now,
                QueueStatus = ApiConstants.QueueStatus.InQueue,
                UserId = "ASW"
            };
        }

        private void MapIncludedNotesFromAsw(List<AswNote> notes, string messageId)
        {
            if (notes == null) return;
            foreach (var n in notes)
            {
                _context.TabMessageThphytoIncludedNotes.Add(new TabMessageThphytoIncludedNote { MessageId = messageId, Subject = n.NoteSubjectCode ?? "N/A", Content = n.NoteContent ?? "" });
            }
        }

        private void MapItemsFromAsw(List<AswDetail> items, string messageId)
        {
            foreach (var item in items)
            {
                string itemId = Guid.NewGuid().ToString();
                _context.TabMessageThphytoItems.Add(new TabMessageThphytoItem { MessageId = messageId, ItemId = itemId, SequenceNo = item.ItemNo, ProductScientName = item.ProductScientName });

                if (item.Descriptions != null)
                    foreach (var d in item.Descriptions) _context.TabMessageThphytoItemDescriptions.Add(new TabMessageThphytoItemDescription { MessageId = messageId, ItemId = itemId, ProductDescription = d.Name ?? "" });

                // Additionals for ASW
                if (item.Additionals != null)
                {
                    foreach (var n in item.Additionals)
                    {
                        string additionalNoteId = Guid.NewGuid().ToString();
                        _context.TabMessageThphytoItemAdditionalNotes.Add(new TabMessageThphytoItemAdditionalNote { MessageId = messageId, ItemId = itemId, AdditionalNoteId = additionalNoteId, Subject = n.NoteSubject ?? "N/A" });
                        _context.TabMessageThphytoItemAdditionalNoteContents.Add(new TabMessageThphytoItemAdditionalNoteContent { MessageId = messageId, ItemId = itemId, AdditionalNoteId = additionalNoteId, NoteContent = n.NoteContent ?? "" });
                    }
                }
            }
        }
        #endregion
    }
}
