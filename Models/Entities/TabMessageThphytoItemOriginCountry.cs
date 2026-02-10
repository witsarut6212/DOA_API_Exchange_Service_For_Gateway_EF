using System;
using System.Collections.Generic;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Entities;

public partial class TabMessageThphytoItemOriginCountry
{
    public int Id { get; set; }

    public string MessageId { get; set; } = null!;

    public string ItemId { get; set; } = null!;

    public string CountryId { get; set; } = null!;

    public string? CountryName { get; set; }

    public string? SubDivisionId { get; set; }

    public string? SubDivisionName { get; set; }

    public string? HeirachiLevel { get; set; }

    public string? AuthorizePartyId { get; set; }

    public string? AuthorizePartyName { get; set; }

    public string? AuthorizeRoleCode { get; set; }

    public string? SpecifyAddrPostCode { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
