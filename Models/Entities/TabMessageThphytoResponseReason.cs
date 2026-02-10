using System;
using System.Collections.Generic;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Entities;

public partial class TabMessageThphytoResponseReason
{
    public int Id { get; set; }

    public int ResponseId { get; set; }

    public string DocId { get; set; } = null!;

    public string ReasonCode { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? SystemTime { get; set; }
}
