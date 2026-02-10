using System;
using System.Collections.Generic;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Entities;

public partial class TabMessageThphytoItemProcess
{
    public int Id { get; set; }

    public string MessageId { get; set; } = null!;

    public string ItemId { get; set; } = null!;

    public string ProcessId { get; set; } = null!;

    public string TypeCode { get; set; } = null!;

    public string? StartDate { get; set; }

    public string? EndDate { get; set; }

    public double? Duration { get; set; }

    public string? DurationUnit { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
