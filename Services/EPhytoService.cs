using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DOA_API_Exchange_Service_For_Gateway.Data;
using DOA_API_Exchange_Service_For_Gateway.Models.Entities;
using DOA_API_Exchange_Service_For_Gateway.Models.Requests;
using Microsoft.EntityFrameworkCore;

namespace DOA_API_Exchange_Service_For_Gateway.Services
{
    public class EPhytoService : IEPhytoService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EPhytoService> _logger;

        public EPhytoService(AppDbContext context, ILogger<EPhytoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> IsDocumentExists(string docId, string docType, string docStatus)
        {
            return await _context.TabMessageThphytos.AnyAsync(t => t.DocId == docId && t.DocType == docType && t.DocStatus == docStatus);
        }

        public async Task<bool> SubmitEPhytoAsync(EPhytoRequest request, string source)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    string messageId = Guid.NewGuid().ToString();
                    
                    // 1. Map and Save Main Document
                    var thphyto = MapToThPhyto(request, messageId, source);
                    _context.TabMessageThphytos.Add(thphyto);

                    // 2. Map and Save Related Data
                    MapIncludedNotes(request.XcDocument.IncludeNotes, messageId);
                    MapReferenceDocs(request, messageId);
                    MapTransportInfo(request.Consignment, messageId);
                    
                    // 3. Map and Save Items
                    MapItems(request.Items, messageId);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error submitting E-Phyto submission from {Source}", source);
                    throw;
                }
            });
        }

        #region Mapping Helpers (Private Methods)

        private TabMessageThphyto MapToThPhyto(EPhytoRequest request, string messageId, string source)
        {
            var doc = request.XcDocument;
            var consignment = request.Consignment;

            // Handle IssueLocation which can be string or object
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
                PhytoTo = source,
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
                QueueStatus = "IN-QUEUE"
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
                    Content = contentStr, 
                    CreatedAt = DateTime.Now 
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
                    PdfObject = r.PdfObject,
                    CreatedAt = DateTime.Now
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
                        SealNumber = ut.SealNumber,
                        CreatedAt = DateTime.Now
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
                        TransportMeanName = mc.TransportMeanName,
                        CreatedAt = DateTime.Now
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
                    ProductScientName = item.ScientName,
                    CreatedAt = DateTime.Now
                });

                MapItemDetails(item, messageId, itemId);
            }
        }

        private void MapItemDetails(EPhytoItem item, string messageId, string itemId)
        {
            // Descriptions
            if (item.Descriptions != null)
            {
                foreach (var d in item.Descriptions)
                {
                    _context.TabMessageThphytoItemDescriptions.Add(new TabMessageThphytoItemDescription
                    {
                        MessageId = messageId,
                        ItemId = itemId,
                        ProductDescription = d.Name ?? "",
                        CreatedAt = DateTime.Now
                    });
                }
            }

            // Common Names
            if (item.CommonNames != null)
            {
                foreach (var c in item.CommonNames)
                {
                    _context.TabMessageThphytoItemCommonNames.Add(new TabMessageThphytoItemCommonName
                    {
                        MessageId = messageId,
                        ItemId = itemId,
                        ProudctCommonName = c.Name ?? "",
                        CreatedAt = DateTime.Now
                    });
                }
            }

            // Additional Notes
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
                    Subject = n.Subject ?? "N/A",
                    CreatedAt = DateTime.Now
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
                            NoteContent = c.Content ?? "",
                            CreatedAt = DateTime.Now
                        });
                    }
                }
            }
        }

        #endregion
    }
}
