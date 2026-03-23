using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using DOA_API_Exchange_Service_For_Gateway.Data;
using DOA_API_Exchange_Service_For_Gateway.Models.Entities;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;
using DOA_API_Exchange_Service_For_Gateway.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    public class EPhytoService : IEPhytoService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EPhytoService> _logger;
        private readonly ILogService _logService;
        private readonly IPreSaveLogger _preSaveLogger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public EPhytoService(AppDbContext context, ILogger<EPhytoService> logger, ILogService logService, IPreSaveLogger preSaveLogger, IConfiguration configuration, IWebHostEnvironment env)
        {
            _context = context;
            _logger = logger;
            _logService = logService;
            _preSaveLogger = preSaveLogger;
            _configuration = configuration;
            _env = env;
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
                        MapClausesFromAsw(aswReq.Clauses, messageId);
                        MapTransitsFromAsw(aswReq.Transits, messageId);
                        MapReferenceDocsFromAsw(aswReq, messageId);
                        MapTransportFromAsw(aswReq, messageId);
                        MapItemsFromAsw(aswReq.Detail, messageId);

                        // --- DEV ONLY: AUDIT LOG ---
                        if (_env.IsDevelopment())
                        {
                            var auditInfo = new {
                                MessageId = messageId,
                                DocId = aswReq.DocId ?? "N/A",
                                NotesCount = aswReq.Notes?.Count ?? 0,
                                ClausesCount = aswReq.Clauses?.Count ?? 0,
                                TransitsCount = aswReq.Transits?.Count ?? 0,
                                ItemsCount = aswReq.Detail?.Count ?? 0,
                                FirstItemDescriptions = aswReq.Detail?.FirstOrDefault()?.Descriptions?.Count ?? 0,
                                Summary = "ASW Mapping Completed successfully. Ready to persist."
                            };
                            await _preSaveLogger.LogAsync(docIdForLog, "ASW_AUDIT_MAPPING", messageId, auditInfo);
                        }

                        await _context.SaveChangesAsync();
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
                        MapUtilizeTransportFromIppc(ippcReq.Consignment?.UtilizeTransport, messageId);
                        MapItemsFromIppc(ippcReq.Items, messageId);

                        await _context.SaveChangesAsync();
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
                ReexportCountryId = consignment.ReexportCountry?.Id,
                ReexportCountryName = consignment.ReexportCountry?.Name,
                UnloadingBasePortId = consignment.UnloadingBasePort?.Id,
                UnloadingBasePortName = consignment.UnloadingBasePort?.Name,
                AuthLocationId = authLocationId,
                AuthLocationName = authLocationName,
                AuthActualDateTime = DateTime.TryParse(doc.SignatoryAuthen?.ActualDatetime, out var adv) ? adv.ToString("yyyy-MM-dd") : doc.SignatoryAuthen?.ActualDatetime,
                AuthProviderId = doc.SignatoryAuthen?.ProviderParty?.Id,
                AuthProviderName = doc.SignatoryAuthen?.ProviderParty?.Name,
                AuthSpecifyPersonName = doc.SignatoryAuthen?.ProviderParty?.SpecfiedPersonName ?? doc.SignatoryAuthen?.ProviderParty?.SpecifiedPerson?.Name,
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

        private void MapUtilizeTransportFromIppc(UtilizeTransportRequest? transport, string messageId)
        {
            if (transport == null) return;
            _context.TabMessageThphytoUtilizeTransports.Add(new TabMessageThphytoUtilizeTransport
            {
                MessageId = messageId,
                EquipmentId = transport.EquipmentId ?? "",
                SealNumber = transport.SealNumber ?? "",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });
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
            if (consignment.MainCarriages != null)
                foreach (var mc in consignment.MainCarriages) _context.TabMessageThphytoMainCarriages.Add(new TabMessageThphytoMainCarriage { MessageId = messageId, TransportModeCode = mc.ModeCode, TransportMeanName = mc.TransportMeanName });
        }

        private void MapItemsFromIppc(List<EPhytoItem> items, string messageId)
        {
            if (items == null) return;
            foreach (var item in items)
            {
                string itemId = Guid.NewGuid().ToString();
                
                decimal? netW = decimal.TryParse(item.NetWeight?.Weight, out var nw) ? nw : (decimal?)null;
                decimal? grossW = decimal.TryParse(item.GrossWeight?.Weight, out var gw) ? gw : (decimal?)null;

                _context.TabMessageThphytoItems.Add(new TabMessageThphytoItem
                {
                    MessageId = messageId,
                    ItemId = itemId,
                    SequenceNo = int.TryParse(item.SequenceNo, out var sn) ? sn : 0,
                    ProductScientName = item.ScientName,
                    NetWeight = netW,
                    NetWeightUnit = item.NetWeight?.UnitCode,
                    GrossWeight = grossW,
                    GrossWeightUnit = item.GrossWeight?.UnitCode
                });

                if (item.Descriptions != null)
                    foreach (var d in item.Descriptions) _context.TabMessageThphytoItemDescriptions.Add(new TabMessageThphytoItemDescription { MessageId = messageId, ItemId = itemId, ProductDescription = d.Name ?? "" });

                if (item.CommonNames != null)
                    foreach (var cn in item.CommonNames) _context.TabMessageThphytoItemCommonNames.Add(new TabMessageThphytoItemCommonName { MessageId = messageId, ItemId = itemId, ProudctCommonName = cn.Name ?? "" });

                if (item.IntendUses != null)
                    foreach (var iu in item.IntendUses) _context.TabMessageThphytoItemIntendeds.Add(new TabMessageThphytoItemIntended { MessageId = messageId, ItemId = itemId, ProductIntendUse = iu.Name ?? "" });

                if (item.OriginCountries != null)
                {
                    foreach (var oc in item.OriginCountries)
                    {
                        _context.TabMessageThphytoItemOriginCountries.Add(new TabMessageThphytoItemOriginCountry 
                        { 
                            MessageId = messageId, 
                            ItemId = itemId, 
                            CountryId = oc.Id ?? "", 
                            CountryName = oc.Name, 
                            SubDivisionId = oc.SubordinaryCountry?.SubdivisionId, 
                            SubDivisionName = oc.SubordinaryCountry?.SubdivisionName 
                        });
                    }
                }

                if (item.PhysicalPackages != null)
                {
                    foreach (var pp in item.PhysicalPackages)
                    {
                        int? qty = int.TryParse(pp.Quantity, out var q) ? q : (int?)null;
                        string marks = pp.ShippingMarks != null ? string.Join(", ", pp.ShippingMarks.Select(sm => sm.Marking)) : "";
                        _context.TabMessageThphytoItemPhysicalPackages.Add(new TabMessageThphytoItemPhysicalPackage 
                        { 
                            MessageId = messageId, 
                            ItemId = itemId, 
                            LevelCode = pp.LevelCode ?? "", 
                            TypeCode = pp.TypeCode ?? "", 
                            ShippingMarks = marks, 
                            Quantity = qty 
                        });
                    }
                }

                if (item.AppliedProcesses != null)
                {
                    foreach (var p in item.AppliedProcesses)
                    {
                        string processId = Guid.NewGuid().ToString();
                        double? duration = double.TryParse(p.DurationMeasure, out var d) ? d : (double?)null;
                        
                        _context.TabMessageThphytoItemProcesses.Add(new TabMessageThphytoItemProcess 
                        { 
                            MessageId = messageId, 
                            ItemId = itemId, 
                            ProcessId = processId, 
                            TypeCode = p.TypeCode ?? "", 
                            StartDate = p.CompletePeriod?.StartDate, 
                            EndDate = p.CompletePeriod?.EndDate, 
                            Duration = duration, 
                            DurationUnit = p.DurationMeasuerUnit ?? "" 
                        });

                        if (p.Characteristics != null)
                        {
                            foreach (var ch in p.Characteristics)
                            {
                                _context.TabMessageThphytoItemProcessCharacteristics.Add(new TabMessageThphytoItemProcessCharacteristic 
                                { 
                                    MessageId = messageId, 
                                    ItemId = itemId, 
                                    ProcessId = processId, 
                                    TypeCode = ch.Value<string>("type_code") ?? "", 
                                    Description1 = ch.Value<string>("description_1") ?? "", 
                                    Description2 = ch.Value<string>("description_2") ?? "", 
                                    ValueMeasure = ch.Value<string>("value_measure") ?? "", 
                                    UnitCode = ch.Value<string>("unit_code") ?? "" 
                                });
                            }
                        }
                    }
                }

                if (item.ApplicableClassifications != null)
                {
                    foreach (var c in item.ApplicableClassifications)
                    {
                        var firstClassName = c.ClassNames?.FirstOrDefault()?.ClassName ?? "";
                        _context.TabMessageThphytoItemApplicableClassifications.Add(new TabMessageThphytoItemApplicableClassification 
                        { 
                            MessageId = messageId, 
                            ItemId = itemId, 
                            ApplicableId = Guid.NewGuid().ToString(),
                            SystemName = c.SystemName ?? "", 
                            ClassCode = c.ClassCode ?? "", 
                            ClassName = firstClassName 
                        });
                    }
                }

                if (item.AdditionalNotes != null)
                {
                    foreach (var n in item.AdditionalNotes)
                    {
                        string additionalNoteId = Guid.NewGuid().ToString();
                        _context.TabMessageThphytoItemAdditionalNotes.Add(new TabMessageThphytoItemAdditionalNote { MessageId = messageId, ItemId = itemId, AdditionalNoteId = additionalNoteId, Subject = n.Subject ?? "N/A" });
                        var contentStr = n.Contents != null ? string.Join(", ", n.Contents.Select(c => c.Content)) : "";
                        _context.TabMessageThphytoItemAdditionalNoteContents.Add(new TabMessageThphytoItemAdditionalNoteContent { MessageId = messageId, ItemId = itemId, AdditionalNoteId = additionalNoteId, NoteContent = contentStr });
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
                DocDescription = request.DocDescription,
                DocType = request.DocType ?? "",
                DocStatus = request.DocStatus ?? "",
                IssueDateTime = DateTime.TryParse(request.RefIssueDatetime, out var dt) ? dt : DateTime.Now,
                IssuerId = request.IssuerId,
                IssuerName = request.IssuerName ?? "N/A",
                RequestDateTime = DateTime.Now,
                ConsignorId = request.ExporterId,
                ConsignorName = request.ExporterName ?? "N/A",
                ConsignorAddrLine1 = request.ExporterLine1,
                ConsignorAddrLine2 = request.ExporterLine2,
                ConsignorAddrLine3 = request.ExporterLine3,
                ConsignorAddrLine4 = request.ExporterLine4,
                ConsignorAddrLine5 = request.ExporterLine5,
                ConsignorCityName = request.ExporterCityName,
                ConsignorPostcode = request.ExporterPostCode,
                ConsignorCountryId = request.ExporterCountryCode,
                ConsignorCounrtyName = request.ExporterCountryName,
                ConsignorTypeCode = request.ExporterAddressType,
                ConsigneeId = request.ConsigneeId,
                ConsigneeName = request.ConsigneeName ?? "N/A",
                ConsigneeAddrLine1 = request.ConsigneeLine1,
                ConsigneeAddrLine2 = request.ConsigneeLine2,
                ConsigneeAddrLine3 = request.ConsigneeLine3,
                ConsigneeAddrLine4 = request.ConsigneeLine4,
                ConsigneeAddrLine5 = request.ConsigneeLine5,
                ConsigneeCityName = request.ConsigneeCityName,
                ConsigneePostcode = request.ConsigneePostCode,
                ConsigneeCountryId = request.ConsigneeCountryCode,
                ConsigneeCountryName = request.ConsigneeCountryName,
                ConsigneeAddressType = request.ConsigneeAddressType,
                ExportCountryId = request.ExportCountryCode ?? "",
                ExportCountryName = request.ExportCountryName,
                ImportCountryId = request.ImportCountryCode ?? "",
                ImportCountryName = request.ImportCountryName,
                UnloadingBasePortId = request.UnloadingPortCode,
                UnloadingBasePortName = request.UnloadingPortName,
                ExamEventOccrurLocationName = request.ExamEventOccurLocationName,
                AuthLocationId = request.AuthLocationId,
                AuthLocationName = request.AuthLocationName,
                AuthProviderId = request.AuthProviderId,
                AuthProviderName = request.AuthProviderName,
                AuthSpecifyPersonName = request.AuthPerson,
                AuthAttainedQualificationName = request.AuthPositionName,
                AuthAbbrevName = request.AuthPositionAbb,
                AuthActualDateTime = request.AuthActualDateTime, 
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

        private void MapClausesFromAsw(List<AswClause> clauses, string messageId)
        {
            if (clauses == null) return;
            foreach (var c in clauses)
            {
                _context.TabMessageThphytoIncludedClauses.Add(new TabMessageThphytoIncludedClause { MessageId = messageId, ClauseId = c.ClauseId, Content = c.ClauseContent });
            }
        }

        private void MapTransitsFromAsw(List<AswTransit> transits, string messageId)
        {
            if (transits == null) return;
            foreach (var t in transits)
            {
                _context.TabMessageThphytoTransitCountries.Add(new TabMessageThphytoTransitCountry
                {
                    MessageId = messageId,
                    CountryId = t.CountryCode ?? "",
                    CountryName = t.CountryName
                });
            }
        }

        private void MapReferenceDocsFromAsw(AswNormalRequest request, string messageId)
        {
            if (!string.IsNullOrEmpty(request.RefId))
            {
                _context.TabMessageThphytoReferenceDocs.Add(new TabMessageThphytoReferenceDoc
                {
                    MessageId = messageId,
                    RefDocId = request.RefId ?? "N/A",
                    TypeCode = request.RefType,
                    RelationTypeCode = request.RefRelation,
                    Information = request.RefInfo
                });
            }
        }

        private void MapTransportFromAsw(AswNormalRequest request, string messageId)
        {
            if (!string.IsNullOrEmpty(request.VesselName) || !string.IsNullOrEmpty(request.TransportMode))
            {
                _context.TabMessageThphytoMainCarriages.Add(new TabMessageThphytoMainCarriage
                {
                    MessageId = messageId,
                    TransportModeCode = request.TransportMode,
                    TransportMeanName = request.VesselName ?? request.VesselNumber
                });
            }
        }

        private void MapItemsFromAsw(List<AswDetail> items, string messageId)
        {
            if (items == null) return;
            foreach (var item in items)
            {
                string itemId = Guid.NewGuid().ToString();
                _context.TabMessageThphytoItems.Add(new TabMessageThphytoItem
                {
                    MessageId = messageId,
                    ItemId = itemId,
                    SequenceNo = item.ItemNo,
                    ProductScientName = item.ProductScientName,
                    ProductBatchId = item.ProductBatchId,
                    NetWeight = item.NetWeight,
                    NetWeightUnit = item.NetWeightUnit,
                    GrossWeight = item.GrossWeight,
                    GrossWeightUnit = item.GrossWeightUnit
                });

                if (item.Descriptions != null)
                    foreach (var d in item.Descriptions) _context.TabMessageThphytoItemDescriptions.Add(new TabMessageThphytoItemDescription { MessageId = messageId, ItemId = itemId, ProductDescription = d.Name ?? "" });

                if (!string.IsNullOrEmpty(item.ProductName))
                    _context.TabMessageThphytoItemCommonNames.Add(new TabMessageThphytoItemCommonName { MessageId = messageId, ItemId = itemId, ProudctCommonName = item.ProductName });

                if (!string.IsNullOrEmpty(item.ProductIntendedUse))
                    _context.TabMessageThphytoItemIntendeds.Add(new TabMessageThphytoItemIntended { MessageId = messageId, ItemId = itemId, ProductIntendUse = item.ProductIntendedUse });

                if (item.OriginCountries != null)
                {
                    foreach (var oc in item.OriginCountries)
                    {
                        _context.TabMessageThphytoItemOriginCountries.Add(new TabMessageThphytoItemOriginCountry { MessageId = messageId, ItemId = itemId, CountryId = oc.OriginCountryCode ?? "", CountryName = oc.OriginCountryName, SubDivisionId = oc.OriginProvinceCode, SubDivisionName = oc.OriginProvinceName, HeirachiLevel = oc.OriginHierarchiLevel, AuthorizePartyId = oc.ProducerCode, AuthorizePartyName = oc.ProducerName, AuthorizeRoleCode = oc.ProducerRole });
                    }
                }

                if (item.Classifications != null)
                {
                    foreach (var c in item.Classifications)
                    {
                        _context.TabMessageThphytoItemApplicableClassifications.Add(new TabMessageThphytoItemApplicableClassification 
                        { 
                            MessageId = messageId, 
                            ItemId = itemId, 
                            ApplicableId = Guid.NewGuid().ToString(),
                            SystemName = c.SystemName ?? "", 
                            ClassCode = c.ClassCode ?? "", 
                            ClassName = c.ClassName ?? "" 
                        });
                    }
                }

                if (item.TransportEquipments != null)
                {
                    foreach (var te in item.TransportEquipments)
                    {
                        _context.TabMessageThphytoItemTransportEquipments.Add(new TabMessageThphytoItemTransportEquipment { MessageId = messageId, ItemId = itemId, TransportId = te.EquipmentId ?? "", SealNumber = te.SealNumber ?? "" });
                    }
                }

                if (!string.IsNullOrEmpty(item.PackageLevel) || !string.IsNullOrEmpty(item.PackageType))
                {
                    _context.TabMessageThphytoItemPhysicalPackages.Add(new TabMessageThphytoItemPhysicalPackage 
                    { 
                        MessageId = messageId, 
                        ItemId = itemId, 
                        LevelCode = item.PackageLevel ?? "", 
                        TypeCode = item.PackageType ?? "", 
                        ShippingMarks = item.ShippingMarks ?? "", 
                        Quantity = (int?)item.PackageAmount 
                    });
                }

                if (item.Processes != null)
                {
                    foreach (var p in item.Processes)
                    {
                        string processId = Guid.NewGuid().ToString();
                        _context.TabMessageThphytoItemProcesses.Add(new TabMessageThphytoItemProcess { MessageId = messageId, ItemId = itemId, ProcessId = processId, TypeCode = p.ProcessType ?? "", StartDate = p.ProcessStartDate, EndDate = p.ProcessEndDate, Duration = (double?)(p.ProcessDuration), DurationUnit = p.ProcessDurationUnit ?? "" });
                        if (p.Characteristics != null)
                        {
                            foreach (var ch in p.Characteristics)
                            {
                                _context.TabMessageThphytoItemProcessCharacteristics.Add(new TabMessageThphytoItemProcessCharacteristic { MessageId = messageId, ItemId = itemId, ProcessId = processId, TypeCode = ch.ProcessCharacterType ?? "", Description1 = ch.ProcessCharacterCode ?? "", Description2 = ch.ProcessCharacterDesc ?? "", ValueMeasure = ch.ProcessValue ?? "", UnitCode = ch.ProcessValueUnit ?? "" });
                            }
                        }
                    }
                }
            }
        }
        #endregion
    }
}
