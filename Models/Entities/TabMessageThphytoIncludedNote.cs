using System;
using System.Collections.Generic;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Entities;

public partial class TabMessageThphytoIncludedNote
{
    public int Id { get; set; }

    public string MessageId { get; set; } = null!;

    public string? NoteId { get; set; }

    public string Subject { get; set; } = null!;

    public string Content { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
