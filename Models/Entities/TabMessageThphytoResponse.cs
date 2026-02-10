using System;
using System.Collections.Generic;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Entities;

public partial class TabMessageThphytoResponse
{
    public int Id { get; set; }

    public string DocId { get; set; } = null!;

    public string DocCode { get; set; } = null!;

    public string? DocName { get; set; }

    public DateTime SubmissionDate { get; set; }

    public string CategoryCode { get; set; } = null!;

    public string ReferenceDocumentId { get; set; } = null!;

    public string ReferenceDocumentCode { get; set; } = null!;

    public DateTime? ReferenceDocumentDate { get; set; }

    public string SenderPartyId { get; set; } = null!;

    public string RecipientPartyId { get; set; } = null!;

    public string IssueCountryId { get; set; } = null!;

    public string? IssueCountryName { get; set; }

    public string? FlagUpdate { get; set; }

    public DateTime? InboundAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? SystemTime { get; set; }

    public string? HeaderMessageId { get; set; }

    public DateTime? HeaderTimeStamp { get; set; }

    public string? HeaderRefMessageId { get; set; }
}
