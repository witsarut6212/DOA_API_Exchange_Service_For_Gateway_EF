using System;
using System.Collections.Generic;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Entities;

public partial class TabMessageThphytoTransitCountry
{
    public int Id { get; set; }

    public string MessageId { get; set; } = null!;

    public string CountryId { get; set; } = null!;

    public string? CountryName { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
