using System;
using System.Collections.Generic;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Entities;

public partial class TabMessageThphytoMainCarriage
{
    public int Id { get; set; }

    public string? MessageId { get; set; }

    /// <summary>
    /// MainCarriageSPSTransportMovement.ModeCode
    /// </summary>
    public string? TransportModeCode { get; set; }

    public string? TransportMeanName { get; set; }

    /// <summary>
    /// MainCarriageSPSTransportMovement.ID
    /// </summary>
    public string? MovementId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
