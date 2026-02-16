using System;
using System.Collections.Generic;

namespace DOA_API_Exchange_Service_For_Gateway.Models.Entities;

public partial class TabMessageThphyto
{
    public int Id { get; set; }

    public string MessageId { get; set; } = null!;

    public string MessageStatus { get; set; } = null!;

    public string PhytoTo { get; set; } = null!;

    public string? DocName { get; set; }

    public string DocId { get; set; } = null!;

    public string? DocDescription { get; set; }

    public string DocType { get; set; } = null!;

    public string DocStatus { get; set; } = null!;

    public DateTime IssueDateTime { get; set; }

    public string? IssuerId { get; set; }

    public string IssuerName { get; set; } = null!;

    public DateTime RequestDateTime { get; set; }

    public string? AuthActualDateTime { get; set; }

    public string? AuthLocationId { get; set; }

    public string? AuthLocationName { get; set; }

    public string? AuthProviderId { get; set; }

    public string? AuthProviderName { get; set; }

    public string? AuthSpecifyPersonName { get; set; }

    public string? AuthAttainedQualificationName { get; set; }

    public string? AuthAbbrevName { get; set; }

    public string? ConsignorId { get; set; }

    public string ConsignorName { get; set; } = null!;

    public string? ConsignorAddrLine1 { get; set; }

    public string? ConsignorAddrLine2 { get; set; }

    public string? ConsignorAddrLine3 { get; set; }

    public string? ConsignorAddrLine4 { get; set; }

    public string? ConsignorAddrLine5 { get; set; }

    public string? ConsignorCityName { get; set; }

    public string? ConsignorPostcode { get; set; }

    public string? ConsignorCountryId { get; set; }

    public string? ConsignorCounrtyName { get; set; }

    public string? ConsignorTypeCode { get; set; }

    public string? ConsigneeId { get; set; }

    public string ConsigneeName { get; set; } = null!;

    public string? ConsigneeAddrLine1 { get; set; }

    public string? ConsigneeAddrLine2 { get; set; }

    public string? ConsigneeAddrLine3 { get; set; }

    public string? ConsigneeAddrLine4 { get; set; }

    public string? ConsigneeAddrLine5 { get; set; }

    public string? ConsigneeCityName { get; set; }

    public string? ConsigneePostcode { get; set; }

    public string? ConsigneeCountryId { get; set; }

    public string? ConsigneeCountryName { get; set; }

    public string? ConsigneeAddressType { get; set; }

    public string ExportCountryId { get; set; } = null!;

    public string? ExportCountryName { get; set; }

    public string? ExportSubordinaryId { get; set; }

    public string? ExportSubordinaryName { get; set; }

    public string? ExportSubordinaryHeirachiLevel { get; set; }

    public string? ReexportCountryId { get; set; }

    public string? ReexportCountryName { get; set; }

    public string ImportCountryId { get; set; } = null!;

    public string? ImportCountryName { get; set; }

    public string? UnloadingBasePortId { get; set; }

    public string? UnloadingBasePortName { get; set; }

    public string? ExamEventOccrurLocationName { get; set; }

    public string ResponseStatus { get; set; } = null!;

    public DateTime? ResponseAt { get; set; }

    public DateTime TimeStamp { get; set; }

    public DateTime LastUpdate { get; set; }

    public string? UserId { get; set; }

    public string? HubDeliveryNumber { get; set; }

    public string? NswMessageId { get; set; }

    public string? MarkSendAsw { get; set; }

    public DateTime? SendDateAsw { get; set; }

    public string? MarkSendAcfs { get; set; }

    public DateTime? SendDateAcfs { get; set; }

    public string? MarkSendIppc { get; set; }

    public DateTime? SendDateIppc { get; set; }

    public string? QueueStatus { get; set; }

    public int? QueueId { get; set; }

    public string? AcfsQueueStatus { get; set; }

    public int? AcfsQueueId { get; set; }
}
