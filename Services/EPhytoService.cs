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
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                string msgId = Guid.NewGuid().ToString();
                var doc = request.XcDocument;
                var consignment = request.Consignment;

                var thphyto = new TabMessageThphyto
                {
                    MessageId = msgId,
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

                    ExportCountryId = consignment.ExportCountryId,
                    ImportCountryId = consignment.ImportCountryId,
                    UnloadingBasePortName = consignment.UnloadingBasePort?.Name,

                    AuthLocationName = doc.SignatoryAuthen?.IssueLocation?.ToString(),
                    AuthProviderName = doc.SignatoryAuthen?.ProviderParty?.Name,
                    AuthActualDateTime = doc.SignatoryAuthen?.ActualDatetime,

                    ResponseStatus = "0101",
                    TimeStamp = DateTime.Now,
                    LastUpdate = DateTime.Now,
                    QueueStatus = "IN-QUEUE"
                };

                _context.TabMessageThphytos.Add(thphyto);

                // Mapping Included Notes
                if (doc.IncludeNotes != null)
                {
                    foreach (var hn in doc.IncludeNotes)
                    {
                        string contentStr = hn.Contents != null 
                            ? string.Join(", ", hn.Contents.Select(c => c.Content)) 
                            : "";
                        
                        _context.TabMessageThphytoIncludedNotes.Add(new TabMessageThphytoIncludedNote 
                        { 
                            MessageId = msgId, 
                            Subject = hn.Subject ?? "N/A", 
                            Content = contentStr, 
                            CreatedAt = DateTime.Now 
                        });
                    }
                }

                // Mapping Reference Docs and PhytoCerts
                var allRefs = new List<ReferenceDocRequest>();
                if (doc.ReferenceDocs != null) allRefs.AddRange(doc.ReferenceDocs);
                if (request.PhytoCerts != null) allRefs.AddRange(request.PhytoCerts);

                foreach (var r in allRefs)
                {
                    _context.TabMessageThphytoReferenceDocs.Add(new TabMessageThphytoReferenceDoc
                    {
                        MessageId = msgId,
                        DocId = doc.DocId,
                        RefDocId = r.DocId ?? r.DocumentNo ?? "",
                        Filename = r.Filename ?? r.Name,
                        PdfObject = r.PdfObject,
                        CreatedAt = DateTime.Now
                    });
                }

                // Mapping Utilize Transport
                if (consignment.UtilizeTransport != null)
                {
                    _context.TabMessageThphytoUtilizeTransports.Add(new TabMessageThphytoUtilizeTransport
                    {
                        MessageId = msgId,
                        SealNumber = consignment.UtilizeTransport.SealNumber,
                        CreatedAt = DateTime.Now
                    });
                }

                // Mapping Main Carriages
                if (consignment.MainCarriages != null)
                {
                    foreach (var mc in consignment.MainCarriages)
                    {
                        _context.TabMessageThphytoMainCarriages.Add(new TabMessageThphytoMainCarriage
                        {
                            MessageId = msgId,
                            TransportModeCode = mc.ModeCode,
                            TransportMeanName = mc.TransportMeanName ?? mc.TrasportMeanName,
                            CreatedAt = DateTime.Now
                        });
                    }
                }

                // Mapping Items
                foreach (var item in request.Items)
                {
                    string itmId = Guid.NewGuid().ToString();
                    _context.TabMessageThphytoItems.Add(new TabMessageThphytoItem
                    {
                        MessageId = msgId,
                        ItemId = itmId,
                        SequenceNo = item.SequenceNo,
                        ProductScientName = item.ScientName,
                        CreatedAt = DateTime.Now
                    });

                    if (item.Descriptions != null)
                    {
                        foreach (var d in item.Descriptions)
                        {
                            string desc = d is Newtonsoft.Json.Linq.JObject obj ? obj["name"]?.ToString() ?? "" : d?.ToString() ?? "";
                            _context.TabMessageThphytoItemDescriptions.Add(new TabMessageThphytoItemDescription
                            {
                                MessageId = msgId,
                                ItemId = itmId,
                                ProductDescription = desc,
                                CreatedAt = DateTime.Now
                            });
                        }
                    }

                    if (item.CommonNames != null)
                    {
                        foreach (var c in item.CommonNames)
                        {
                            string name = c is Newtonsoft.Json.Linq.JObject obj ? obj["name"]?.ToString() ?? "" : c?.ToString() ?? "";
                            _context.TabMessageThphytoItemCommonNames.Add(new TabMessageThphytoItemCommonName
                            {
                                MessageId = msgId,
                                ItemId = itmId,
                                ProudctCommonName = name,
                                CreatedAt = DateTime.Now
                            });
                        }
                    }

                    if (item.AdditionalNotes != null)
                    {
                        foreach (var n in item.AdditionalNotes)
                        {
                            string nid = Guid.NewGuid().ToString();
                            _context.TabMessageThphytoItemAdditionalNotes.Add(new TabMessageThphytoItemAdditionalNote
                            {
                                MessageId = msgId,
                                ItemId = itmId,
                                AdditionalNoteId = nid,
                                Subject = n.Subject ?? "N/A",
                                CreatedAt = DateTime.Now
                            });

                            if (n.Contents != null)
                            {
                                foreach (var c in n.Contents)
                                {
                                    _context.TabMessageThphytoItemAdditionalNoteContents.Add(new TabMessageThphytoItemAdditionalNoteContent
                                    {
                                        MessageId = msgId,
                                        ItemId = itmId,
                                        AdditionalNoteId = nid,
                                        NoteContent = c.Content ?? "",
                                        CreatedAt = DateTime.Now
                                    });
                                }
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error submitting E-Phyto");
                throw;
            }
        }
    }
}
