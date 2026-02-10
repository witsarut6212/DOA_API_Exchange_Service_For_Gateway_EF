using System;
using System.Collections.Generic;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Entities;

public partial class TabMessageThphytoItem
{
    public int Id { get; set; }

    public string MessageId { get; set; } = null!;

    public string ItemId { get; set; } = null!;

    public int SequenceNo { get; set; }

    public string? ProductScientName { get; set; }

    public string? ProductBatchId { get; set; }

    public decimal? NetWeight { get; set; }

    public string? NetWeightUnit { get; set; }

    public decimal? GrossWeight { get; set; }

    public string? GrossWeightUnit { get; set; }

    public double? NetVolume { get; set; }

    public string? NetVolumeUnit { get; set; }

    public double? GrossVolume { get; set; }

    public string? GrossVolumeUnit { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
