using System;
using System.Collections.Generic;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Entities;

public partial class TabMessageThphytoReferenceDoc
{
    public int Id { get; set; }

    public string MessageId { get; set; } = null!;

    public string RefDocId { get; set; } = null!;

    public DateTime? IssueDate { get; set; }

    public string? TypeCode { get; set; }

    public string? RelationTypeCode { get; set; }

    public string? DocId { get; set; }

    public string? Filename { get; set; }

    public string? Information { get; set; }

    public string? PdfObject { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
