using System;
using System.Collections.Generic;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Entities;

public partial class TabMessageRepsonsePayload
{
    public int Id { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public string DataObject { get; set; } = null!;
}
