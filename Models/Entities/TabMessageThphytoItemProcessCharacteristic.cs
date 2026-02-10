using System;
using System.Collections.Generic;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Entities;

public partial class TabMessageThphytoItemProcessCharacteristic
{
    public int Id { get; set; }

    public string MessageId { get; set; } = null!;

    public string ItemId { get; set; } = null!;

    public string ProcessId { get; set; } = null!;

    public string? TypeCode { get; set; }

    public string? Description1 { get; set; }

    public string? Description2 { get; set; }

    public string? ValueMeasure { get; set; }

    public string? UnitCode { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
