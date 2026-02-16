using System;
using System.Collections.Generic;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Entities;

public partial class TabMessageThphytoMainCarriage
{
    public int Id { get; set; }

    public string? MessageId { get; set; }

    public string? TransportModeCode { get; set; }

    public string? TransportMeanName { get; set; }

    public string? MovementId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
