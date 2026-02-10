using System;
using System.Collections.Generic;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Entities;

public partial class TabMessageThphytoUtilizeTransport
{
    /// <summary>
    /// ID Records
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ID of THPHYTO
    /// </summary>
    public string MessageId { get; set; } = null!;

    /// <summary>
    /// UtilizedSPSTransportEquipment.ID
    /// </summary>
    public string? EquipmentId { get; set; }

    /// <summary>
    /// AffixedSPSSeal.ID
    /// </summary>
    public string? SealNumber { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
